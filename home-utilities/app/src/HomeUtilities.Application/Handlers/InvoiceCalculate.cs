using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Authorization;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomeUtilities.Application.Common;
using HomeUtilities.Application.Services;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Handlers;

namespace HomeUtilities.Application.Handlers;

[KandraHandlerBehavior(typeof(InvoiceCalculateHandler))]
public sealed class InvoiceCalculateBehavior(
    IEntityRepository<ServiceNode> servicesRepository,
    IEntityRepository<Meter> metersRepository,
    IEntityRepository<Property> propertiesRepository,
    IEntityRepository<Provider> providersRepository,
    IInfoRegisterReader<MeterReadingRecord> readingsReader,
    IInfoRegisterReader<TariffRecord> tariffsReader,
    IAuthorizationChecker? authorizationChecker = null,
    ICurrentTimeZone? currentTimeZone = null)
    : IHandlerBehavior<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>
{
    public async Task<InvoiceCalculateResponseDto> HandleAsync(InvoiceCalculateRequestDto request, CancellationToken cancellationToken)
    {
        authorizationChecker?.AuthorizeFrom(typeof(InvoiceCalculateHandler));

        if (request.ProviderId == Guid.Empty || request.PropertyId == Guid.Empty)
            return new InvoiceCalculateResponseDto();

        var monthStart = TimeZoneHelper.ToMonthStartUtc(request.Month, currentTimeZone?.TimeZone);
        var (localMonthStartUtc, monthEnd) = TimeZoneHelper.LocalMonthBoundsUtc(monthStart, currentTimeZone?.TimeZone);

        var property = await propertiesRepository.GetAsync(request.PropertyId, false, cancellationToken);
        var propertyArea = property?.Area ?? 0m;

        var services = new List<Service>();
        await foreach (var s in servicesRepository.GetAllAsync(q => q.OfType<Service>()
            .Where(s => s.ProviderId == request.ProviderId && !s.IsDeleted)))
        {
            if (s is Service service)
            {
                services.Add(service);
            }
        }

        var meters = new List<Meter>();
        await foreach (var m in metersRepository.GetAllAsync(q => q.Where(m => m.PropertyId == request.PropertyId && !m.IsDeleted)))
        {
            meters.Add(m);
        }

        var lines = new List<InvoiceCalculateLineDto>();

        foreach (var service in services)
        {
            if (service.Billing == BillingType.Metered)
            {
                var serviceMeters = meters.Where(m => m.ServiceId == service.Id).ToList();
                foreach (var meter in serviceMeters)
                {
                    if (meter.Zones == MeterZones.Single)
                    {
                        var qty = await CalculateZoneQuantityAsync(meter.Id, TariffZone.Single, localMonthStartUtc, monthEnd, cancellationToken);

                        var tariffSlice = await tariffsReader.SliceOfLast(monthEnd,
                            q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Single),
                            cancellationToken);
                        var price = tariffSlice.Count > 0 ? tariffSlice[0].Details.Price : 0m;
                        var amt = Math.Round(qty * price, 2);

                        lines.Add(new InvoiceCalculateLineDto
                        {
                            ServiceId = service.Id,
                            MeterId = meter.Id,
                            Zone = TariffZone.Single,
                            Quantity = qty,
                            Price = price,
                            Amount = amt,
                        });
                    }
                    else // DayNight
                    {
                        // Day
                        var qtyDay = await CalculateZoneQuantityAsync(meter.Id, TariffZone.Day, localMonthStartUtc, monthEnd, cancellationToken);

                        var tariffSliceDay = await tariffsReader.SliceOfLast(monthEnd,
                            q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Day),
                            cancellationToken);
                        var priceDay = tariffSliceDay.Count > 0 ? tariffSliceDay[0].Details.Price : 0m;
                        var amtDay = Math.Round(qtyDay * priceDay, 2);

                        lines.Add(new InvoiceCalculateLineDto
                        {
                            ServiceId = service.Id,
                            MeterId = meter.Id,
                            Zone = TariffZone.Day,
                            Quantity = qtyDay,
                            Price = priceDay,
                            Amount = amtDay,
                        });

                        // Night
                        var qtyNight = await CalculateZoneQuantityAsync(meter.Id, TariffZone.Night, localMonthStartUtc, monthEnd, cancellationToken);

                        var tariffSliceNight = await tariffsReader.SliceOfLast(monthEnd,
                            q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Night),
                            cancellationToken);
                        var priceNight = tariffSliceNight.Count > 0 ? tariffSliceNight[0].Details.Price : 0m;
                        var amtNight = Math.Round(qtyNight * priceNight, 2);

                        lines.Add(new InvoiceCalculateLineDto
                        {
                            ServiceId = service.Id,
                            MeterId = meter.Id,
                            Zone = TariffZone.Night,
                            Quantity = qtyNight,
                            Price = priceNight,
                            Amount = amtNight,
                        });
                    }
                }
            }
            else if (service.Billing == BillingType.FixedPerMonth)
            {
                var tariffSlice = await tariffsReader.SliceOfLast(monthEnd,
                    q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Single),
                    cancellationToken);
                var price = tariffSlice.Count > 0 ? tariffSlice[0].Details.Price : 0m;
                var amt = Math.Round(1m * price, 2);

                lines.Add(new InvoiceCalculateLineDto
                {
                    ServiceId = service.Id,
                    MeterId = null,
                    Zone = TariffZone.Single,
                    Quantity = 1m,
                    Price = price,
                    Amount = amt,
                });
            }
            else if (service.Billing == BillingType.PerSquareMetre)
            {
                var tariffSlice = await tariffsReader.SliceOfLast(monthEnd,
                    q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Single),
                    cancellationToken);
                var price = tariffSlice.Count > 0 ? tariffSlice[0].Details.Price : 0m;
                var amt = Math.Round(propertyArea * price, 2);

                lines.Add(new InvoiceCalculateLineDto
                {
                    ServiceId = service.Id,
                    MeterId = null,
                    Zone = TariffZone.Single,
                    Quantity = propertyArea,
                    Price = price,
                    Amount = amt,
                });
            }
            else if (service.Billing == BillingType.PerPerson)
            {
                var personsCount = property?.RegisteredPersonsCount ?? 1;
                var tariffSlice = await tariffsReader.SliceOfLast(monthEnd,
                    q => q.Where(r => r.Dimensions.ServiceId == service.Id && r.Dimensions.Zone == TariffZone.Single),
                    cancellationToken);
                var price = tariffSlice.Count > 0 ? tariffSlice[0].Details.Price : 0m;
                var amt = Math.Round((decimal)personsCount * price, 2);

                lines.Add(new InvoiceCalculateLineDto
                {
                    ServiceId = service.Id,
                    MeterId = null,
                    Zone = TariffZone.Single,
                    Quantity = personsCount,
                    Price = price,
                    Amount = amt,
                });
            }
        }

        var meterSummaries = new List<string>();
        foreach (var l in lines.Where(l => l.MeterId.HasValue))
        {
            var meter = meters.FirstOrDefault(m => m.Id == l.MeterId!.Value);
            var mName = meter?.Name ?? "Meter";
            var zoneStr = l.Zone != TariffZone.Single ? $" ({l.Zone})" : "";
            meterSummaries.Add($"{mName}{zoneStr}: {l.Quantity:G29}");
        }
        var meterSummary = string.Join("; ", meterSummaries);

        var provider = await providersRepository.GetAsync(request.ProviderId, false, cancellationToken);
        var paymentDescription = PaymentDescriptionFormatter.Format(
            provider?.PaymentDescriptionTemplate,
            provider,
            property,
            monthStart,
            meterReadingsSummary: meterSummary);

        return new InvoiceCalculateResponseDto
        {
            Lines = lines,
            TotalAmount = lines.Sum(l => l.Amount),
            PaymentDescription = paymentDescription,
        };
    }

    private async Task<decimal> CalculateZoneQuantityAsync(
        Guid meterId,
        TariffZone zone,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken)
    {
        var lastInMonth = await readingsReader.SliceOfLast(monthEnd,
            q => q.Where(r => r.Dimensions.MeterId == meterId && r.Dimensions.Zone == zone),
            cancellationToken);

        if (lastInMonth.Count == 0 || lastInMonth[0].Period < monthStart)
            return 0m;

        var endVal = lastInMonth[0].Details.Reading;
        var beforeMonth = await readingsReader.SliceOfLast(monthStart.AddTicks(-1),
            q => q.Where(r => r.Dimensions.MeterId == meterId && r.Dimensions.Zone == zone),
            cancellationToken);

        decimal startVal;
        if (beforeMonth.Count > 0)
        {
            startVal = beforeMonth[0].Details.Reading;
        }
        else
        {
            var lastBefore = await readingsReader.SliceOfLast(lastInMonth[0].Period.AddTicks(-1),
                q => q.Where(r => r.Dimensions.MeterId == meterId && r.Dimensions.Zone == zone),
                cancellationToken);
            startVal = lastBefore.Count > 0 ? lastBefore[0].Details.Reading : 0m;
        }

        return Math.Max(0m, endVal - startVal);
    }
}
