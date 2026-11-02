using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Forms.Documents;
using HomeUtilities.Forms.Handlers;
using HomeUtilities.Tests.Support;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Services;
using Kandra.Domain.Exceptions;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.Extensions.DependencyInjection;

namespace HomeUtilities.Tests.Acceptance;

/// <summary>
/// The rules the adoption review found broken: the billing month for a user west of UTC, an amount typed from the
/// provider's invoice, a missing tariff, the payment purpose without a template, and a backdated meter reading.
/// </summary>
[Collection("DataImport")]
public sealed class ReviewFixesTests
{
    /// <summary>New York (UTC-5, UTC-4 in summer): local midnight of a date is 04:00 or 05:00 UTC of the same day.</summary>
    private static readonly TimeZoneInfo WestOfUtc =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/New_York", out var ny) ? ny : TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

    private static IDocumentCrudService<InvoiceDto, InvoiceQueryDto> Invoices(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<InvoiceDto, InvoiceQueryDto>>();

    private static IDocumentCrudService<MeterReadingDto, MeterReadingQueryDto> MeterReadings(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<MeterReadingDto, MeterReadingQueryDto>>();

    private static IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto> Calculate(IServiceProvider sp) =>
        sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate");

    /// <summary>Local midnight of a date in <paramref name="zone"/>, as the client sends a picked date: a UTC instant.</summary>
    private static DateTime LocalMidnightUtc(int year, int month, int day, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified), zone);

    private static async Task<(Guid Property, Guid Provider, Guid Service)> SeedAsync(
        HomeUtilitiesTestHost host, BillingType billing, string? template = null)
    {
        var ct = CancellationToken.None;
        var property = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<PropertyDto, PropertyQueryDto>>()
            .InsertAsync(new PropertyDto { Code = "P1", Name = "Flat", Address = "Main St 1", Area = 50m }, ct));
        var provider = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<ProviderDto, ProviderQueryDto>>()
            .InsertAsync(new ProviderDto
            {
                Code = "PRV1", Name = "Power Co", Kind = ProviderKind.Electricity, PersonalAccountNumber = "ACC-01", Phone = "1",
                PaymentDescriptionTemplate = template,
            }, ct));
        var service = await host.InScopeAsync(sp => sp.GetRequiredService<IHierarchicalDictionaryCrudService<ServiceNodeDto, ServiceQueryDto>>()
            .InsertAsync(new ServiceDto { Code = "SRV1", Name = "Electricity", Unit = "kWh", Billing = billing, ProviderId = provider.Id }, ct));
        return (property.Id, provider.Id, service.Id);
    }

    [Fact]
    public async Task InvoiceMonth_ForUserWestOfUtc_StaysTheMonthPicked_ThroughSaveAndRepost()
    {
        using var host = new HomeUtilitiesTestHost(timeZone: WestOfUtc);
        var ct = CancellationToken.None;
        var (property, provider, service) = await SeedAsync(host, BillingType.FixedPerMonth);

        var invoice = await host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV1",
            Date = LocalMidnightUtc(2026, 11, 5, WestOfUtc),
            PropertyId = property,
            ProviderId = provider,
            Month = LocalMidnightUtc(2026, 11, 1, WestOfUtc),
            Lines = [new InvoiceLineDto { ServiceId = service, Zone = TariffZone.Single, Quantity = 1m, Price = 100m, Amount = 100m }],
        }, ct));
        await host.InScopeAsync(sp => Invoices(sp).SubmitAsync(invoice.Id, ct));

        // Save the posted invoice again: it re-posts, and the month must not move.
        var loaded = await host.InScopeAsync(sp => Invoices(sp).GetByIdAAsync(invoice.Id, ct));
        await host.InScopeAsync(sp => Invoices(sp).UpdateAsync(loaded, ct));
        var reloaded = await host.InScopeAsync(sp => Invoices(sp).GetByIdAAsync(invoice.Id, ct));

        Assert.Equal(LocalMidnightUtc(2026, 11, 1, WestOfUtc), reloaded.Month);
        var movements = await host.InScopeAsync(sp => sp.GetRequiredService<ITurnoverReader<UtilityCostMovement>>()
            .Movements(DateTime.MinValue, DateTime.MaxValue, q => q, ct));
        var movement = Assert.Single(movements);
        Assert.Equal(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), movement.Period);
    }

    [Fact]
    public async Task InvoiceLine_AmountTypedFromProvidersInvoice_IsKept_EmptyAmountIsComputed()
    {
        using var host = new HomeUtilitiesTestHost();
        var ct = CancellationToken.None;
        var (property, provider, service) = await SeedAsync(host, BillingType.FixedPerMonth);

        var invoice = await host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV1",
            Date = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property,
            ProviderId = provider,
            Month = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines =
            [
                new InvoiceLineDto { ServiceId = service, Zone = TariffZone.Single, Quantity = 100m, Price = 4.3219m, Amount = 432.17m },
                new InvoiceLineDto { ServiceId = service, Zone = TariffZone.Single, Quantity = 2m, Price = 10m },
            ],
        }, ct));

        var saved = await host.InScopeAsync(sp => Invoices(sp).GetByIdAAsync(invoice.Id, ct));
        Assert.Contains(saved.Lines, l => l.Amount == 432.17m);
        Assert.Contains(saved.Lines, l => l.Amount == 20m);
        Assert.Equal(452.17m, saved.TotalAmount);
    }

    [Fact]
    public async Task Calculate_WithoutAValidTariff_RefusesInsteadOfPriceZero()
    {
        using var host = new HomeUtilitiesTestHost();
        var (property, provider, _) = await SeedAsync(host, BillingType.FixedPerMonth);

        var error = await Assert.ThrowsAsync<InvalidStateException>(() => host.InScopeAsync(sp => Calculate(sp).HandleAsync(
            new InvoiceCalculateRequestDto
            {
                PropertyId = property, ProviderId = provider, Month = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            }, CancellationToken.None)));
        Assert.Contains("Electricity", error.Message);
    }

    [Fact]
    public async Task PaymentPurpose_WithoutTemplate_ComesFromTheResources_NotHardCodedUkrainian()
    {
        using var host = new HomeUtilitiesTestHost();
        var ct = CancellationToken.None;
        var (property, provider, service) = await SeedAsync(host, BillingType.FixedPerMonth);

        var invoice = await host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV1",
            Date = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property,
            ProviderId = provider,
            Month = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines = [new InvoiceLineDto { ServiceId = service, Zone = TariffZone.Single, Quantity = 1m, Price = 100m, Amount = 100m }],
        }, ct));

        var saved = await host.InScopeAsync(sp => Invoices(sp).GetByIdAAsync(invoice.Id, ct));
        Assert.Equal("Utility services for 11.2026; account ACC-01; Main St 1", saved.PaymentDescription);
    }

    [Fact]
    public async Task BackdatedReading_HigherThanTheNextReading_RefusesThePost()
    {
        using var host = new HomeUtilitiesTestHost();
        var ct = CancellationToken.None;
        var (property, _, service) = await SeedAsync(host, BillingType.Metered);
        var meter = await host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<MeterDto, MeterQueryDto>>()
            .InsertAsync(new MeterDto
            {
                Code = "M1", Name = "El Meter", PropertyId = property, ServiceId = service, SerialNumber = "SN1",
                Zones = MeterZones.Single, NextVerificationDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            }, ct));

        async Task<Guid> ReadingAsync(string code, DateTime date, decimal value)
        {
            var doc = await host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
            {
                Code = code, Date = date, PropertyId = property,
                Lines = [new MeterReadingLineDto { MeterId = meter.Id, Zone = TariffZone.Single, Reading = value }],
            }, ct));
            return doc.Id;
        }

        var november = await ReadingAsync("MR1", new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc), 1500m);
        await host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(november, ct));

        var october = await ReadingAsync("MR2", new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc), 1600m);
        await Assert.ThrowsAsync<InvalidStateException>(() => host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(october, ct)));

        var octoberLower = await ReadingAsync("MR3", new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), 1400m);
        await host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(octoberLower, ct));
    }
}
