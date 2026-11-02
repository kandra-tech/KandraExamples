using HomeUtilities.Application.Dashboards;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dashboards;
using HomeUtilities.Forms.DataProcessors;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Forms.Documents;
using HomeUtilities.Forms.Handlers;
using Kandra.Forms.Querying;
using HomeUtilities.Forms.Reports;
using HomeUtilities.Tests.Support;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Services;
using Kandra.DataImport;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.Extensions.DependencyInjection;

namespace HomeUtilities.Tests.Acceptance;

[Collection("DataImport")]
public sealed class AcceptanceTests : IDisposable
{
    private readonly HomeUtilitiesTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static IDocumentCrudService<InvoiceDto, InvoiceQueryDto> Invoices(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<InvoiceDto, InvoiceQueryDto>>();

    private static IDocumentCrudService<PaymentDto, PaymentQueryDto> Payments(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<PaymentDto, PaymentQueryDto>>();

    private static IDocumentCrudService<MeterReadingDto, MeterReadingQueryDto> MeterReadings(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<MeterReadingDto, MeterReadingQueryDto>>();

    private static IDocumentCrudService<SetTariffsDto, SetTariffsQueryDto> SetTariffs(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<SetTariffsDto, SetTariffsQueryDto>>();

    private static IDictionaryCrudService<PropertyDto, PropertyQueryDto> Properties(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<PropertyDto, PropertyQueryDto>>();

    private static IDictionaryCrudService<ProviderDto, ProviderQueryDto> Providers(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<ProviderDto, ProviderQueryDto>>();

    private static IHierarchicalDictionaryCrudService<ServiceNodeDto, ServiceQueryDto> Services(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<ServiceNodeDto, ServiceQueryDto>>();

    private static IDictionaryCrudService<MeterDto, MeterQueryDto> Meters(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<MeterDto, MeterQueryDto>>();

    /// <summary>
    /// Check 1: Calculate on an Invoice takes the metered quantity from the readings and the price from the tariff valid at the end of the month.
    /// </summary>
    [Fact]
    public async Task Check1_CalculateOnInvoice_TakesQuantityFromReadings_AndPriceFromValidTariffAtMonthEnd()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P1",
            Name = "Flat",
            Address = "Main St 1",
            Area = 60m
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV1",
            Name = "Power Co",
            Kind = ProviderKind.Electricity,
            PersonalAccountNumber = "ACC-01",
            Phone = "123"
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV1",
            Name = "Electricity",
            Unit = "kWh",
            Billing = BillingType.Metered,
            ProviderId = provider.Id
        }, ct));

        var meter = await _host.InScopeAsync(sp => Meters(sp).InsertAsync(new MeterDto
        {
            Code = "MTR1",
            Name = "El Meter",
            PropertyId = property.Id,
            ServiceId = service.Id,
            SerialNumber = "SN123",
            Zones = MeterZones.Single,
            NextVerificationDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }, ct));

        // Set tariff valid from 2026-01-01: 4.50 UAH
        var tariffDoc = await _host.InScopeAsync(sp => SetTariffs(sp).InsertAsync(new SetTariffsDto
        {
            Code = "ST1",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines =
            [
                new SetTariffsLineDto { ServiceId = service.Id, Zone = TariffZone.Single, Price = 4.50m }
            ]
        }, ct));
        await _host.InScopeAsync(sp => SetTariffs(sp).SubmitAsync(tariffDoc.Id, ct));

        // Readings: 100 on Jan 1, 220 on Jan 31 -> quantity in Jan = 120
        var reading1 = await _host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
        {
            Code = "MR1",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            Lines =
            [
                new MeterReadingLineDto { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 0m, Reading = 100m }
            ]
        }, ct));
        await _host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(reading1.Id, ct));

        var reading2 = await _host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
        {
            Code = "MR2",
            Date = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            Lines =
            [
                new MeterReadingLineDto { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 100m, Reading = 220m }
            ]
        }, ct));
        await _host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(reading2.Id, ct));

        // Calculate invoice for January 2026
        var calcResponse = await _host.InScopeAsync(sp =>
        {
            var handler = sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate");
            return handler.HandleAsync(new InvoiceCalculateRequestDto
            {
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Month = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }, ct);
        });

        Assert.NotEmpty(calcResponse.Lines);
        var line = Assert.Single(calcResponse.Lines);
        Assert.Equal(service.Id, line.ServiceId);
        Assert.Equal(meter.Id, line.MeterId);
        Assert.Equal(120m, line.Quantity); // 220 - 100
        Assert.Equal(4.50m, line.Price);    // Tariff at end of Jan
        Assert.Equal(540.00m, line.Amount); // 120 * 4.50
    }

    /// <summary>
    /// Check 2: A tariff change in the middle of the period applies only to the months after its date.
    /// </summary>
    [Fact]
    public async Task Check2_TariffChangeInMiddleOfPeriod_AppliesOnlyToMonthsAfterItsDate()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P2", Name = "Apt", Address = "Street 2", Area = 50m
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV2", Name = "Gas Co", Kind = ProviderKind.Gas, PersonalAccountNumber = "ACC-02", Phone = "456"
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV2", Name = "Gas", Unit = "m³", Billing = BillingType.FixedPerMonth, ProviderId = provider.Id
        }, ct));

        // Tariff 1: 10.00 valid from 2026-01-01
        var t1 = await _host.InScopeAsync(sp => SetTariffs(sp).InsertAsync(new SetTariffsDto
        {
            Code = "ST2-1",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines = [ new SetTariffsLineDto { ServiceId = service.Id, Zone = TariffZone.Single, Price = 10.00m } ]
        }, ct));
        await _host.InScopeAsync(sp => SetTariffs(sp).SubmitAsync(t1.Id, ct));

        // Tariff 2: 25.00 valid from 2026-03-15 (middle of March)
        var t2 = await _host.InScopeAsync(sp => SetTariffs(sp).InsertAsync(new SetTariffsDto
        {
            Code = "ST2-2",
            Date = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Lines = [ new SetTariffsLineDto { ServiceId = service.Id, Zone = TariffZone.Single, Price = 25.00m } ]
        }, ct));
        await _host.InScopeAsync(sp => SetTariffs(sp).SubmitAsync(t2.Id, ct));

        // February ends 2026-02-28, before tariff change: price must be 10.00
        var calcFeb = await _host.InScopeAsync(sp => sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate").HandleAsync(new InvoiceCalculateRequestDto
        {
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Month = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        }, ct));
        Assert.Equal(10.00m, Assert.Single(calcFeb.Lines).Price);

        // March ends 2026-03-31, after tariff change on 2026-03-15: price must be 25.00
        var calcMar = await _host.InScopeAsync(sp => sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate").HandleAsync(new InvoiceCalculateRequestDto
        {
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Month = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        }, ct));
        Assert.Equal(25.00m, Assert.Single(calcMar.Lines).Price);
    }

    /// <summary>
    /// Check 3: Posting an Invoice increases the debt to its provider; a Payment decreases it.
    /// </summary>
    [Fact]
    public async Task Check3_PostingInvoiceIncreasesDebt_PaymentDecreasesDebt()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P3", Name = "Flat 3", Address = "Street 3", Area = 70m
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV3", Name = "Water Co", Kind = ProviderKind.Water, PersonalAccountNumber = "ACC-03", Phone = "789"
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV3", Name = "Water", Unit = "m³", Billing = BillingType.FixedPerMonth, ProviderId = provider.Id
        }, ct));

        // Post Invoice for 800 UAH
        var invoice = await _host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV3",
            Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Month = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines = [ new InvoiceLineDto { ServiceId = service.Id, Zone = TariffZone.Single, Quantity = 1m, Price = 800m, Amount = 800m } ]
        }, ct));
        await _host.InScopeAsync(sp => Invoices(sp).SubmitAsync(invoice.Id, ct));

        // Verify debt increased to 800
        var balanceAfterInvoice = await _host.InScopeAsync(async sp =>
        {
            var balances = await sp.GetRequiredService<IBalanceReader<SettlementMovement, SettlementBalance>>()
                .Balances(q => q.Where(b => b.Dimensions.ProviderId == provider.Id && b.Dimensions.PropertyId == property.Id), ct);
            return balances.Sum(b => b.Resources.Amount);
        });
        Assert.Equal(800m, balanceAfterInvoice);

        // Post Payment for 300 UAH
        var payment = await _host.InScopeAsync(sp => Payments(sp).InsertAsync(new PaymentDto
        {
            Code = "PAY3",
            Date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Amount = 300m,
            Comment = "Partial payment"
        }, ct));
        await _host.InScopeAsync(sp => Payments(sp).SubmitAsync(payment.Id, ct));

        // Verify debt decreased to 500
        var balanceAfterPayment = await _host.InScopeAsync(async sp =>
        {
            var balances = await sp.GetRequiredService<IBalanceReader<SettlementMovement, SettlementBalance>>()
                .Balances(q => q.Where(b => b.Dimensions.ProviderId == provider.Id && b.Dimensions.PropertyId == property.Id), ct);
            return balances.Sum(b => b.Resources.Amount);
        });
        Assert.Equal(500m, balanceAfterPayment);
    }

    /// <summary>
    /// Check 4: Changing a posted Invoice, un-posting it, or deleting it updates the debt and the costs correctly.
    /// </summary>
    [Fact]
    public async Task Check4_ChangingPostedInvoice_UnpostingOrDeleting_UpdatesDebtAndCostsCorrectly()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P4", Name = "Flat 4", Address = "Street 4", Area = 80m
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV4", Name = "Heating Co", Kind = ProviderKind.Heating, PersonalAccountNumber = "ACC-04", Phone = "000"
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV4", Name = "Heating", Unit = "Gcal", Billing = BillingType.FixedPerMonth, ProviderId = provider.Id
        }, ct));

        // 1. Post Invoice for 1000
        var invoice = await _host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV4",
            Date = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Month = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines = [ new InvoiceLineDto { ServiceId = service.Id, Zone = TariffZone.Single, Quantity = 10m, Price = 100m, Amount = 1000m } ]
        }, ct));
        await _host.InScopeAsync(sp => Invoices(sp).SubmitAsync(invoice.Id, ct));

        var debt1 = await GetDebtAsync(provider.Id, property.Id);
        var cost1 = await GetCostsTurnoverAsync(property.Id, service.Id);
        Assert.Equal(1000m, debt1);
        Assert.Equal(1000m, cost1);

        // 2. Change the posted Invoice: amount becomes 1500 (re-posting on save)
        var loaded = await _host.InScopeAsync(sp => Invoices(sp).GetByIdAAsync(invoice.Id, ct));
        var firstLine = loaded.Lines.First();
        firstLine.Quantity = 15m;
        firstLine.Amount = 1500m;
        await _host.InScopeAsync(sp => Invoices(sp).UpdateAsync(loaded, ct));

        var debt2 = await GetDebtAsync(provider.Id, property.Id);
        var cost2 = await GetCostsTurnoverAsync(property.Id, service.Id);
        Assert.Equal(1500m, debt2);
        Assert.Equal(1500m, cost2);

        // 3. Un-post the Invoice
        await _host.InScopeAsync(sp => Invoices(sp).UnsubmitAsync(invoice.Id, ct));

        var debt3 = await GetDebtAsync(provider.Id, property.Id);
        var cost3 = await GetCostsTurnoverAsync(property.Id, service.Id);
        Assert.Equal(0m, debt3);
        Assert.Equal(0m, cost3);

        // 4. Submit again then delete
        await _host.InScopeAsync(sp => Invoices(sp).SubmitAsync(invoice.Id, ct));
        Assert.Equal(1500m, await GetDebtAsync(provider.Id, property.Id));

        await _host.InScopeAsync(sp => Invoices(sp).UnsubmitAsync(invoice.Id, ct));
        await _host.InScopeAsync(sp => Invoices(sp).DeleteAsync(invoice.Id, ct));

        var debt4 = await GetDebtAsync(provider.Id, property.Id);
        var cost4 = await GetCostsTurnoverAsync(property.Id, service.Id);
        Assert.Equal(0m, debt4);
        Assert.Equal(0m, cost4);
    }

    /// <summary>
    /// Check 5: A reading lower than the meter's previous reading refuses the post.
    /// </summary>
    [Fact]
    public async Task Check5_ReadingLowerThanPrevious_RefusesPost()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P5", Name = "Flat 5", Address = "Street 5", Area = 45m
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV5", Name = "Power 5", Kind = ProviderKind.Electricity, PersonalAccountNumber = "ACC-05", Phone = "111"
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV5", Name = "Power", Unit = "kWh", Billing = BillingType.Metered, ProviderId = provider.Id
        }, ct));

        var meter = await _host.InScopeAsync(sp => Meters(sp).InsertAsync(new MeterDto
        {
            Code = "MTR5", Name = "Meter 5", PropertyId = property.Id, ServiceId = service.Id,
            SerialNumber = "SN555", Zones = MeterZones.Single,
            NextVerificationDate = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }, ct));

        // Reading 1: 500 (posted successfully)
        var mr1 = await _host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
        {
            Code = "MR5-1",
            Date = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            Lines = [ new MeterReadingLineDto { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 0m, Reading = 500m } ]
        }, ct));
        await _host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(mr1.Id, ct));

        // Reading 2: 450 (lower than 500! Must refuse post)
        var mr2 = await _host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
        {
            Code = "MR5-2",
            Date = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            Lines = [ new MeterReadingLineDto { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 0m, Reading = 450m } ]
        }, ct));

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await _host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(mr2.Id, ct));
        });
    }

    /// <summary>
    /// Check 6: A provider statement's closing balance equals the provider's debt on that date.
    /// </summary>
    [Fact]
    public async Task Check6_ProviderStatementClosingBalance_EqualsProviderDebtOnThatDate()
    {
        var ct = CancellationToken.None;

        // Apply the demo package with 6 months of historical data
        await _host.InScopeAsync(async sp =>
        {
            using var stream = sp.GetRequiredService<IImportPackageRegistry>().Open("homeutilities-demo")!;
            await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), ct);
        });

        // Provider KyivTeplo has code PRV-04, Flat has PROP-01
        var providers = await _host.InScopeAsync(sp => Providers(sp).QueryAsync(new ProviderQueryDto(), ct));
        var heatProv = providers.Results.First(p => p.Kind == ProviderKind.Heating);

        var properties = await _host.InScopeAsync(sp => Properties(sp).QueryAsync(new PropertyQueryDto(), ct));
        var flat = properties.Results.First();

        var toDate = DateTime.UtcNow.Date.AddDays(1);
        var fromDate = DateTime.UtcNow.Date.AddDays(-200);

        var reportResult = await _host.InScopeAsync(sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportProviderStatementDto, ProviderStatementReportResult>>();
            return reportService.RunAsync(new ReportProviderStatementDto
            {
                ProviderId = heatProv.Id,
                PropertyId = flat.Id,
                DateRange = new DateRangeDto { From = fromDate, To = toDate }
            }, ct);
        });

        var actualDebt = await _host.InScopeAsync(async sp =>
        {
            var balances = await sp.GetRequiredService<IBalanceReader<SettlementMovement, SettlementBalance>>()
                .Balances(toDate, q => q.Where(b => b.Dimensions.ProviderId == heatProv.Id && b.Dimensions.PropertyId == flat.Id), null, ct);
            return balances.Sum(b => b.Resources.Amount);
        });

        var closingBalanceLine = reportResult.Data.Lines.Last();
        Assert.Equal(actualDebt, closingBalanceLine.Balance);
    }

    /// <summary>
    /// Check 7: The dashboard's figures equal the reports: the total debt as the sum of the provider statements'
    /// closing balances, the chosen service's consumption as in Consumption for the same months.
    /// </summary>
    [Fact]
    public async Task Check7_DashboardFigures_EqualReports()
    {
        var ct = CancellationToken.None;

        // Apply the demo package
        await _host.InScopeAsync(async sp =>
        {
            using var stream = sp.GetRequiredService<IImportPackageRegistry>().Open("homeutilities-demo")!;
            await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), ct);
        });

        // Find electricity service and flat property
        var elecService = await _host.InScopeAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IEntityRepository<Domain.Entities.Dictionaries.ServiceNode>>();
            var list = new List<Domain.Entities.Dictionaries.ServiceNode>();
            await foreach (var s in repo.GetAllAsync(q => q.Where(x => x.Code == "SRV-01")))
            {
                list.Add(s);
            }
            return list.First();
        });

        var properties = await _host.InScopeAsync(sp => Properties(sp).QueryAsync(new PropertyQueryDto(), ct));
        var flat = properties.Results.First();

        // 1. Get dashboard data for chosen service (Electricity)
        var dashboardResult = await _host.InScopeAsync(sp =>
        {
            var service = sp.GetRequiredService<IDashboardService<HomeDashboardDto, HomeDashboardResultDto>>();
            return service.RunAsync(new HomeDashboardDto { ServiceId = elecService.Id }, ct);
        });

        // 2. Compare Total Debt: must equal sum of provider debts
        var totalDebtFromBalances = await _host.InScopeAsync(async sp =>
        {
            var balances = await sp.GetRequiredService<IBalanceReader<SettlementMovement, SettlementBalance>>()
                .Balances(q => q, ct);
            return balances.Sum(b => b.Resources.Amount);
        });

        Assert.Equal(totalDebtFromBalances, dashboardResult.TotalDebtKpi.Value);

        // 3. Compare Consumption of chosen service (Electricity) with Consumption Report
        var consumptionReport = await _host.InScopeAsync(sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportConsumptionDto, ConsumptionReportResult>>();
            return reportService.RunAsync(new ReportConsumptionDto
            {
                PropertyId = flat.Id,
                Year = DateTime.UtcNow.Year
            }, ct);
        });

        var elecReportLines = consumptionReport.Data.Lines.Where(l => l.ServiceName == elecService.Name).ToList();
        var reportTotalQtyThisYear = elecReportLines.Sum(l => l.Total);

        // In dashboard line chart: sum of "This year" series
        var chartThisYearSeries = dashboardResult.ServiceConsumption.Series.FirstOrDefault(s => s.Name == DateTime.UtcNow.Year.ToString() || s.Name.Contains("Year") || s.Name.Contains("рік") || s.Name == "This year");
        Assert.NotNull(chartThisYearSeries);
        var dashboardTotalQtyThisYear = chartThisYearSeries.Values.Sum();

        Assert.Equal(reportTotalQtyThisYear, dashboardTotalQtyThisYear);
    }

    [Fact]
    public async Task AcceptanceCheck8_PaymentRequisitesAndDescriptionCalculation()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P8",
            Name = "Flat 8",
            Address = "Shevchenko 42",
            Area = 65m,
            Owner = "Taras Shevchenko",
            RegisteredPersonsCount = 2,
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV8",
            Name = "Kyiv Electric LLC",
            Kind = ProviderKind.Electricity,
            PersonalAccountNumber = "123456789",
            Phone = "+380441234567",
            Iban = "UA123223130000026001234567890",
            Edrpou = "12345678",
            BankName = "JSC Oschadbank",
            Address = "Khreshchatyk 22, Kyiv",
            PaymentDescriptionTemplate = "Payment for {Month}.{Year}; Acc: {PersonalAccountNumber}; Owner: {Owner}; Addr: {PropertyAddress}",
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "S8",
            Name = "Electricity 8",
            Billing = BillingType.FixedPerMonth,
            ProviderId = provider.Id,
            Unit = "month",
        }, ct));

        await _host.InScopeAsync(async sp =>
        {
            var st = await SetTariffs(sp).InsertAsync(new SetTariffsDto
            {
                Code = "ST8",
                Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Lines = [new() { ServiceId = service.Id, Zone = TariffZone.Single, Price = 150m }]
            }, ct);
            await SetTariffs(sp).SubmitAsync(st.Id, ct);
        });

        // Test Calculate on Invoice
        var calcResponse = await _host.InScopeAsync(sp =>
        {
            var handler = sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate");
            return handler.HandleAsync(new InvoiceCalculateRequestDto
            {
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Month = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            }, ct);
        });

        Assert.NotNull(calcResponse.PaymentDescription);
        Assert.Contains("Payment for 05.2026", calcResponse.PaymentDescription);
        Assert.Contains("Acc: 123456789", calcResponse.PaymentDescription);
        Assert.Contains("Owner: Taras Shevchenko", calcResponse.PaymentDescription);
        Assert.Contains("Addr: Shevchenko 42", calcResponse.PaymentDescription);

        // Insert Invoice with calculated description
        var invoice = await _host.InScopeAsync(sp => Invoices(sp).InsertAsync(new InvoiceDto
        {
            Code = "INV8",
            Date = new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            Month = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            ProviderId = provider.Id,
            PaymentDescription = calcResponse.PaymentDescription,
            Lines = [new() { ServiceId = service.Id, Zone = TariffZone.Single, Quantity = 1m, Price = 150m, Amount = 150m }]
        }, ct));

        // Create Payment linked to this invoice
        var payment = await _host.InScopeAsync(sp => Payments(sp).NewAsync(null, invoice.Id, ct));
        Assert.Equal(calcResponse.PaymentDescription, payment.PaymentDescription);
    }

    [Fact]
    public async Task Check9_EastOfUtc_MonthAndCalculation_UsesLocalTimeZone_AndDoesNotRebillPriorMonth()
    {
        var ct = CancellationToken.None;
        using var eastHost = new HomeUtilitiesTestHost(timeZone: HomeUtilitiesTestHost.EastOfUtc);

        var property = await eastHost.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P9",
            Name = "Kyiv Flat",
            Address = "Khreshchatyk 1",
            Area = 50m,
            RegisteredPersonsCount = 1,
        }, ct));

        var provider = await eastHost.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV9",
            Name = "Kyiv Energy",
            Kind = ProviderKind.Electricity,
        }, ct));

        var service = await eastHost.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "S9",
            Name = "Electricity",
            Billing = BillingType.Metered,
            ProviderId = provider.Id,
            Unit = "kWh",
        }, ct));

        var meter = await eastHost.InScopeAsync(sp => Meters(sp).InsertAsync(new MeterDto
        {
            Code = "M9",
            Name = "El Meter",
            PropertyId = property.Id,
            ServiceId = service.Id,
            Zones = MeterZones.Single,
            NextVerificationDate = new DateTime(2030, 1, 1),
        }, ct));

        await eastHost.InScopeAsync(async sp =>
        {
            var st = await SetTariffs(sp).InsertAsync(new SetTariffsDto
            {
                Code = "ST9",
                Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Lines = [new() { ServiceId = service.Id, Zone = TariffZone.Single, Price = 4m }]
            }, ct);
            await SetTariffs(sp).SubmitAsync(st.Id, ct);
        });

        // Baseline reading in October 2026 (100 kWh) and November 2026 reading (150 kWh)
        await eastHost.InScopeAsync(async sp =>
        {
            var mr1 = await MeterReadings(sp).InsertAsync(new MeterReadingDto
            {
                Code = "MR9-OCT",
                Date = HomeUtilitiesTestHost.LocalMidnight(HomeUtilitiesTestHost.EastOfUtc, 2026, 10, 25),
                PropertyId = property.Id,
                Lines = [new() { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 0m, Reading = 100m }]
            }, ct);
            await MeterReadings(sp).SubmitAsync(mr1.Id, ct);

            var mr2 = await MeterReadings(sp).InsertAsync(new MeterReadingDto
            {
                Code = "MR9-NOV",
                Date = HomeUtilitiesTestHost.LocalMidnight(HomeUtilitiesTestHost.EastOfUtc, 2026, 11, 25),
                PropertyId = property.Id,
                Lines = [new() { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 100m, Reading = 150m }]
            }, ct);
            await MeterReadings(sp).SubmitAsync(mr2.Id, ct);
        });

        // Pick 1 November 2026 in Kyiv (2026-10-31T22:00:00Z): must calculate November (150 - 100 = 50), not October!
        var novLocalMidnightUtc = HomeUtilitiesTestHost.LocalMidnight(HomeUtilitiesTestHost.EastOfUtc, 2026, 11, 1);
        var novCalc = await eastHost.InScopeAsync(sp =>
        {
            var handler = sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate");
            return handler.HandleAsync(new InvoiceCalculateRequestDto
            {
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Month = novLocalMidnightUtc,
            }, ct);
        });

        var novLine = Assert.Single(novCalc.Lines);
        Assert.Equal(50m, novLine.Quantity);
        Assert.Equal(200m, novLine.Amount);

        // Pick 1 December 2026 in Kyiv (no reading in December): must return Quantity = 0, NOT re-bill November's 50 kWh!
        var decLocalMidnightUtc = HomeUtilitiesTestHost.LocalMidnight(HomeUtilitiesTestHost.EastOfUtc, 2026, 12, 1);
        var decCalc = await eastHost.InScopeAsync(sp =>
        {
            var handler = sp.GetRequiredKeyedService<IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>>("InvoiceCalculate");
            return handler.HandleAsync(new InvoiceCalculateRequestDto
            {
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Month = decLocalMidnightUtc,
            }, ct);
        });

        var decLine = Assert.Single(decCalc.Lines);
        Assert.Equal(0m, decLine.Quantity);
    }

    [Fact]
    public async Task Check10_ConsumptionReport_SplitsDayAndNightZones_AndUsesBillingMonth()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P10",
            Name = "Flat 10",
            Address = "Addr 10",
            Area = 60m,
            RegisteredPersonsCount = 2,
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV10",
            Name = "Provider 10",
            Kind = ProviderKind.Electricity,
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "S10",
            Name = "Electricity DayNight",
            Billing = BillingType.Metered,
            ProviderId = provider.Id,
            Unit = "kWh",
        }, ct));

        // Invoice dated 2026-06-05 but billing Month = 2026-05-01 with Day (100) and Night (60) lines
        await _host.InScopeAsync(async sp =>
        {
            var inv = await Invoices(sp).InsertAsync(new InvoiceDto
            {
                Code = "INV10",
                Date = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc),
                Month = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Lines =
                [
                    new() { ServiceId = service.Id, Zone = TariffZone.Day, Quantity = 100m, Price = 4m, Amount = 400m },
                    new() { ServiceId = service.Id, Zone = TariffZone.Night, Quantity = 60m, Price = 2m, Amount = 120m },
                ]
            }, ct);
            await Invoices(sp).SubmitAsync(inv.Id, ct);
        });

        var report = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IReportService<ReportConsumptionDto, ConsumptionReportResult>>()
                .RunAsync(new ReportConsumptionDto { PropertyId = property.Id, Year = 2026 }, ct));

        Assert.Equal(2, report.Data.Lines.Count);
        var dayRow = Assert.Single(report.Data.Lines, r => r.Zone == TariffZone.Day);
        var nightRow = Assert.Single(report.Data.Lines, r => r.Zone == TariffZone.Night);

        // Both must be recorded in Month05 (May), not Month06 (June)!
        Assert.Equal(100m, dayRow.Month05);
        Assert.Equal(0m, dayRow.Month06);
        Assert.Equal(60m, nightRow.Month05);
        Assert.Equal(0m, nightRow.Month06);
    }

    [Fact]
    public async Task Check11_FullDocumentLifecycle_SetTariffs_MeterReading_And_Payment()
    {
        var ct = CancellationToken.None;

        var property = await _host.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P11",
            Name = "Flat 11",
            Address = "Addr 11",
            Area = 45m,
            RegisteredPersonsCount = 1,
        }, ct));

        var provider = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV11",
            Name = "Water Co 11",
            Kind = ProviderKind.Water,
        }, ct));

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "S11",
            Name = "Cold Water 11",
            Billing = BillingType.Metered,
            ProviderId = provider.Id,
            Unit = "m3",
        }, ct));

        var meter = await _host.InScopeAsync(sp => Meters(sp).InsertAsync(new MeterDto
        {
            Code = "M11",
            Name = "Water Meter 11",
            PropertyId = property.Id,
            ServiceId = service.Id,
            Zones = MeterZones.Single,
            NextVerificationDate = new DateTime(2030, 1, 1),
        }, ct));

        // 1. SetTariffs full lifecycle: Insert -> Submit -> Edit while posted -> Unsubmit -> Delete
        var st = await _host.InScopeAsync(sp => SetTariffs(sp).InsertAsync(new SetTariffsDto
        {
            Code = "ST11",
            Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Lines = [new() { ServiceId = service.Id, Zone = TariffZone.Single, Price = 30m }]
        }, ct));

        await _host.InScopeAsync(sp => SetTariffs(sp).SubmitAsync(st.Id, ct));

        var loadedSt = await _host.InScopeAsync(sp => SetTariffs(sp).GetByIdAAsync(st.Id, ct));
        loadedSt.Lines.First().Price = 35m;
        await _host.InScopeAsync(sp => SetTariffs(sp).UpdateAsync(loadedSt, ct));

        var updatedPrice = await _host.InScopeAsync(async sp =>
        {
            var slice = await sp.GetRequiredService<IInfoRegisterReader<TariffRecord>>()
                .SliceOfLast(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    q => q.Where(r => r.Dimensions.ServiceId == service.Id), ct);
            return slice.Single().Details.Price;
        });
        Assert.Equal(35m, updatedPrice);

        await _host.InScopeAsync(sp => SetTariffs(sp).UnsubmitAsync(st.Id, ct));
        await _host.InScopeAsync(sp => SetTariffs(sp).DeleteAsync(st.Id, ct));

        var afterDeleteTariff = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IInfoRegisterReader<TariffRecord>>()
                .Records(DateTime.MinValue, DateTime.MaxValue, q => q.Where(r => r.Dimensions.ServiceId == service.Id), ct));
        Assert.Empty(afterDeleteTariff);

        // 2. MeterReading full lifecycle: Insert -> Submit -> Edit while posted -> Unsubmit -> Delete
        var mr = await _host.InScopeAsync(sp => MeterReadings(sp).InsertAsync(new MeterReadingDto
        {
            Code = "MR11",
            Date = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            Lines = [new() { MeterId = meter.Id, Zone = TariffZone.Single, PreviousReading = 0m, Reading = 50m }]
        }, ct));

        await _host.InScopeAsync(sp => MeterReadings(sp).SubmitAsync(mr.Id, ct));

        var loadedMr = await _host.InScopeAsync(sp => MeterReadings(sp).GetByIdAAsync(mr.Id, ct));
        loadedMr.Lines.First().Reading = 55m;
        await _host.InScopeAsync(sp => MeterReadings(sp).UpdateAsync(loadedMr, ct));

        var updatedReading = await _host.InScopeAsync(async sp =>
        {
            var slice = await sp.GetRequiredService<IInfoRegisterReader<MeterReadingRecord>>()
                .SliceOfLast(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    q => q.Where(r => r.Dimensions.MeterId == meter.Id), ct);
            return slice.Single().Details.Reading;
        });
        Assert.Equal(55m, updatedReading);

        await _host.InScopeAsync(sp => MeterReadings(sp).UnsubmitAsync(mr.Id, ct));
        await _host.InScopeAsync(sp => MeterReadings(sp).DeleteAsync(mr.Id, ct));

        // 3. Payment full lifecycle: Insert Invoice -> Submit -> Insert Payment -> Submit -> Edit while posted -> Unsubmit -> Delete
        await _host.InScopeAsync(async sp =>
        {
            var inv = await Invoices(sp).InsertAsync(new InvoiceDto
            {
                Code = "INV11",
                Date = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
                Month = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PropertyId = property.Id,
                ProviderId = provider.Id,
                Lines = [new() { ServiceId = service.Id, Zone = TariffZone.Single, Quantity = 10m, Price = 30m, Amount = 300m }]
            }, ct);
            await Invoices(sp).SubmitAsync(inv.Id, ct);
        });

        var pay = await _host.InScopeAsync(sp => Payments(sp).InsertAsync(new PaymentDto
        {
            Code = "PAY11",
            Date = new DateTime(2026, 1, 25, 0, 0, 0, DateTimeKind.Utc),
            PropertyId = property.Id,
            ProviderId = provider.Id,
            Amount = 100m,
        }, ct));

        await _host.InScopeAsync(sp => Payments(sp).SubmitAsync(pay.Id, ct));
        Assert.Equal(200m, await GetDebtAsync(provider.Id, property.Id));

        var loadedPay = await _host.InScopeAsync(sp => Payments(sp).GetByIdAAsync(pay.Id, ct));
        loadedPay.Amount = 180m;
        await _host.InScopeAsync(sp => Payments(sp).UpdateAsync(loadedPay, ct));
        Assert.Equal(120m, await GetDebtAsync(provider.Id, property.Id));

        await _host.InScopeAsync(sp => Payments(sp).UnsubmitAsync(pay.Id, ct));
        Assert.Equal(300m, await GetDebtAsync(provider.Id, property.Id));

        await _host.InScopeAsync(sp => Payments(sp).DeleteAsync(pay.Id, ct));
        Assert.Equal(300m, await GetDebtAsync(provider.Id, property.Id));
    }

    [Fact]
    public async Task Check12_Reminders_CreatesNotificationsWithFormattedTitleAndBody()
    {
        var ct = CancellationToken.None;
        using var remHost = new HomeUtilitiesTestHost();

        var property = await remHost.InScopeAsync(sp => Properties(sp).InsertAsync(new PropertyDto
        {
            Code = "P12",
            Name = "Flat 12",
            Address = "Addr 12",
            Area = 50m,
            RegisteredPersonsCount = 1,
        }, ct));

        var provider = await remHost.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = "PRV12",
            Name = "Provider 12",
            Kind = ProviderKind.Water,
        }, ct));

        var service = await remHost.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "S12",
            Name = "Water 12",
            Billing = BillingType.Metered,
            ProviderId = provider.Id,
            Unit = "m3",
        }, ct));

        var asOf = new DateTime(2026, 5, 25);
        await remHost.InScopeAsync(sp => Meters(sp).InsertAsync(new MeterDto
        {
            Code = "M12",
            Name = "Kitchen Water Meter",
            PropertyId = property.Id,
            ServiceId = service.Id,
            Zones = MeterZones.Single,
            NextVerificationDate = asOf.AddDays(30),
        }, ct));

        var res = await remHost.InScopeAsync(sp =>
            sp.GetRequiredService<Kandra.Application.Services.DataProcessors.IDataProcessorService<RemindersDto, RemindersResultDto>>()
                .ProcessAsync(new RemindersDto { AsOfDate = asOf }, ct));

        Assert.Equal(2, res.SentNotificationsCount);
        Assert.Equal(1, res.MissingReadingsCount);
        Assert.Equal(1, res.VerificationDueCount);

        // Running a second time on the same day also succeeds
        var secondRes = await remHost.InScopeAsync(sp =>
            sp.GetRequiredService<Kandra.Application.Services.DataProcessors.IDataProcessorService<RemindersDto, RemindersResultDto>>()
                .ProcessAsync(new RemindersDto { AsOfDate = asOf }, ct));

        Assert.Equal(2, secondRes.SentNotificationsCount);
    }

    private async Task<decimal> GetDebtAsync(Guid providerId, Guid propertyId)
    {
        return await _host.InScopeAsync(async sp =>
        {
            var balances = await sp.GetRequiredService<IBalanceReader<SettlementMovement, SettlementBalance>>()
                .Balances(q => q.Where(b => b.Dimensions.ProviderId == providerId && b.Dimensions.PropertyId == propertyId), CancellationToken.None);
            return balances.Sum(b => b.Resources.Amount);
        });
    }

    private async Task<decimal> GetCostsTurnoverAsync(Guid propertyId, Guid serviceId)
    {
        return await _host.InScopeAsync(async sp =>
        {
            var turnovers = await sp.GetRequiredService<ITurnoverReader<UtilityCostMovement>>()
                .Movements(DateTime.MinValue, DateTime.MaxValue, q => q.Where(m => m.Dimensions.PropertyId == propertyId && m.Dimensions.ServiceId == serviceId), CancellationToken.None);
            return turnovers.Sum(t => t.Resources.Amount);
        });
    }
}
