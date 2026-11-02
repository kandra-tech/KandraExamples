using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Commons;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Domain.Entities.ReportingModels;
using HomeUtilities.Enums;

namespace HomeUtilities.Persistence.Repositories;

internal class HomeUtilitiesReportsRepository(
    IBalanceReader<SettlementMovement, SettlementBalance> settlementsReader,
    ITurnoverReader<UtilityCostMovement> costsReader,
    IInfoRegisterReader<TariffRecord> tariffsReader,
    IInfoRegisterReader<MeterReadingRecord> readingsReader,
    IEntityRepository<ServiceNode> servicesRepo,
    IEntityRepository<Property> propertiesRepo,
    IEntityRepository<Meter> metersRepo) : IReportsRepository
{
    public async Task<List<ProviderStatementLine>> GetProviderStatementAsync(
        Guid providerId,
        Guid propertyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.MaxValue;
        var openingCutoff = from > DateTime.MinValue ? from.AddTicks(-1) : from;

        var openingBalances = await settlementsReader.Balances(openingCutoff,
            q => q.Where(b => b.Dimensions.ProviderId == providerId && b.Dimensions.PropertyId == propertyId),
            q => q.Where(m => m.Dimensions.ProviderId == providerId && m.Dimensions.PropertyId == propertyId),
            cancellationToken);

        var openingDebt = openingBalances.Sum(b => b.Resources.Amount);

        var result = new List<ProviderStatementLine>();

        // Opening row
        result.Add(new ProviderStatementLine
        {
            Date = from == DateTime.MinValue ? DateTime.Today : from,
            Kind = StatementRowKind.OpeningBalance,
            DocumentCode = string.Empty,
            Invoiced = 0m,
            Paid = 0m,
            Balance = openingDebt,
        });

        var movements = await settlementsReader.Movements(from, to,
            q => q.Where(m => m.Dimensions.ProviderId == providerId && m.Dimensions.PropertyId == propertyId)
                  .Include(m => m.Document),
            cancellationToken);

        var orderedMovements = movements.OrderBy(m => m.Period).ThenBy(m => m.DocumentId).ToList();

        var runningBalance = openingDebt;

        foreach (var mov in orderedMovements)
        {
            var docCode = mov.Document?.Code ?? string.Empty;

            if (!mov.IsExpense)
            {
                var invoiced = mov.Resources.Amount;
                runningBalance += invoiced;
                result.Add(new ProviderStatementLine
                {
                    Date = mov.Period,
                    Kind = StatementRowKind.Invoice,
                    DocumentCode = docCode,
                    Invoiced = invoiced,
                    Paid = 0m,
                    Balance = runningBalance,
                });
            }
            else
            {
                var paid = mov.Resources.Amount;
                runningBalance -= paid;
                result.Add(new ProviderStatementLine
                {
                    Date = mov.Period,
                    Kind = StatementRowKind.Payment,
                    DocumentCode = docCode,
                    Invoiced = 0m,
                    Paid = paid,
                    Balance = runningBalance,
                });
            }
        }

        // Closing row
        result.Add(new ProviderStatementLine
        {
            Date = to == DateTime.MaxValue ? DateTime.Today : to,
            Kind = StatementRowKind.ClosingBalance,
            DocumentCode = string.Empty,
            Invoiced = 0m,
            Paid = 0m,
            Balance = runningBalance,
        });

        return result;
    }

    public async Task<List<ConsumptionReportLine>> GetConsumptionAsync(
        Guid propertyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31, 23, 59, 59);
        var prevYearStart = new DateTime(year - 1, 1, 1);
        var prevYearEnd = new DateTime(year - 1, 12, 31, 23, 59, 59);

        var currentMovements = await costsReader.Movements(yearStart, yearEnd,
            q => q.Where(m => m.Dimensions.PropertyId == propertyId), cancellationToken);

        var prevMovements = await costsReader.Movements(prevYearStart, prevYearEnd,
            q => q.Where(m => m.Dimensions.PropertyId == propertyId), cancellationToken);

        var allServices = new List<Service>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.OfType<Service>().Where(x => !x.IsDeleted)))
        {
            if (s is Service service)
            {
                allServices.Add(service);
            }
        }

        var serviceMap = allServices.ToDictionary(s => s.Id);

        var currentByService = currentMovements.GroupBy(m => new { m.Dimensions.ServiceId, m.Dimensions.Zone });
        var prevByService = prevMovements.GroupBy(m => new { m.Dimensions.ServiceId, m.Dimensions.Zone })
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Resources.Quantity));

        var result = new List<ConsumptionReportLine>();

        foreach (var group in currentByService)
        {
            serviceMap.TryGetValue(group.Key.ServiceId, out var service);
            var serviceName = service?.Name ?? string.Empty;

            var monthQuantities = new decimal[12];
            foreach (var mov in group)
            {
                var monthIdx = mov.Period.Month - 1;
                if (monthIdx >= 0 && monthIdx < 12)
                {
                    monthQuantities[monthIdx] += mov.Resources.Quantity;
                }
            }

            var total = monthQuantities.Sum();
            prevByService.TryGetValue(group.Key, out var prevTotal);
            var change = prevTotal == 0m ? 0m : Math.Round((total - prevTotal) / prevTotal * 100m, 2);

            result.Add(new ConsumptionReportLine
            {
                ServiceId = group.Key.ServiceId,
                ServiceName = serviceName,
                Zone = group.Key.Zone,
                Month01 = monthQuantities[0],
                Month02 = monthQuantities[1],
                Month03 = monthQuantities[2],
                Month04 = monthQuantities[3],
                Month05 = monthQuantities[4],
                Month06 = monthQuantities[5],
                Month07 = monthQuantities[6],
                Month08 = monthQuantities[7],
                Month09 = monthQuantities[8],
                Month10 = monthQuantities[9],
                Month11 = monthQuantities[10],
                Month12 = monthQuantities[11],
                Total = total,
                PreviousYearTotal = prevTotal,
                ChangeYoY = change,
            });
        }

        return result.OrderBy(r => r.ServiceName).ThenBy(r => r.Zone).ToList();
    }

    public async Task<List<CostsReportLine>> GetCostsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.MaxValue;

        var movements = await costsReader.Movements(from, to, q => q, cancellationToken);

        var allServices = new List<ServiceNode>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allServices.Add(s);
        }
        var serviceMap = allServices.ToDictionary(s => s.Id);

        var allProperties = new List<Property>();
        await foreach (var p in propertiesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allProperties.Add(p);
        }
        var propertyMap = allProperties.ToDictionary(p => p.Id);

        var grouped = movements.GroupBy(m => new { m.Dimensions.PropertyId, m.Dimensions.ServiceId });
        var result = new List<CostsReportLine>();

        foreach (var g in grouped)
        {
            var propId = g.Key.PropertyId;
            var servId = g.Key.ServiceId;
            var sum = g.Sum(m => m.Resources.Amount);

            propertyMap.TryGetValue(propId, out var prop);
            var propName = prop?.Name ?? string.Empty;

            var groupId = Guid.Empty;
            var groupName = string.Empty;
            var servName = string.Empty;

            if (serviceMap.TryGetValue(servId, out var servNode))
            {
                servName = servNode.Name;
                if (servNode is Service serv && serv.ParentId.HasValue && serviceMap.TryGetValue(serv.ParentId.Value, out var parentNode))
                {
                    groupId = parentNode.Id;
                    groupName = parentNode.Name;
                }
                else
                {
                    groupId = servNode.Id;
                    groupName = servNode.Name;
                }
            }

            result.Add(new CostsReportLine
            {
                PropertyId = propId,
                PropertyName = propName,
                ServiceGroupId = groupId,
                ServiceGroupName = groupName,
                ServiceId = servId,
                ServiceName = servName,
                Amount = sum,
            });
        }

        return result.OrderBy(r => r.PropertyName).ThenBy(r => r.ServiceGroupName).ThenBy(r => r.ServiceName).ToList();
    }

    public async Task<List<TariffHistoryReportLine>> GetTariffHistoryAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var records = await tariffsReader.Records(DateTime.MinValue, DateTime.MaxValue,
            q => q.Where(t => t.Dimensions.ServiceId == serviceId), cancellationToken);

        var ordered = records.OrderBy(r => r.Dimensions.Zone).ThenBy(r => r.Period).ToList();

        var result = new List<TariffHistoryReportLine>();
        var prevByZone = new Dictionary<TariffZone, decimal>();

        foreach (var rec in ordered)
        {
            var zone = rec.Dimensions.Zone;
            var price = rec.Details.Price;

            decimal? change = null;
            if (prevByZone.TryGetValue(zone, out var prevPrice) && prevPrice > 0m)
            {
                change = Math.Round((price - prevPrice) / prevPrice * 100m, 2);
            }
            prevByZone[zone] = price;

            result.Add(new TariffHistoryReportLine
            {
                ServiceId = serviceId,
                Date = rec.Period,
                Zone = zone,
                Price = price,
                ChangePercent = change,
            });
        }

        return result.OrderBy(r => r.Date).ThenBy(r => r.Zone).ToList();
    }

    public async Task<List<MeterConsumptionReportLine>> GetMeterConsumptionAsync(
        Guid propertyId,
        int year,
        CancellationToken cancellationToken = default)
    {
        var meters = new List<Meter>();
        await foreach (var m in metersRepo.GetAllAsync(q => q.Where(m => m.PropertyId == propertyId && !m.IsDeleted)))
        {
            meters.Add(m);
        }

        var allServices = new List<ServiceNode>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allServices.Add(s);
        }
        var serviceMap = allServices.ToDictionary(s => s.Id);

        var result = new List<MeterConsumptionReportLine>();

        foreach (var meter in meters)
        {
            var unit = string.Empty;
            if (serviceMap.TryGetValue(meter.ServiceId, out var sNode) && sNode is Service s)
            {
                unit = s.Unit ?? string.Empty;
            }

            var zones = meter.Zones == MeterZones.DayNight
                ? new[] { TariffZone.Day, TariffZone.Night }
                : new[] { TariffZone.Single };

            foreach (var zone in zones)
            {
                var monthly = new decimal[12];
                for (var m = 1; m <= 12; m++)
                {
                    var monthStart = new DateTime(year, m, 1);
                    var monthEnd = new DateTime(year, m, DateTime.DaysInMonth(year, m), 23, 59, 59);
                    var lastInMonth = await readingsReader.SliceOfLast(monthEnd,
                        q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == zone),
                        cancellationToken);

                    if (lastInMonth.Count > 0 && lastInMonth[0].Period >= monthStart)
                    {
                        var beforeMonth = await readingsReader.SliceOfLast(monthStart.AddTicks(-1),
                            q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == zone),
                            cancellationToken);

                        decimal startVal;
                        if (beforeMonth.Count > 0)
                        {
                            startVal = beforeMonth[0].Details.Reading;
                        }
                        else
                        {
                            var lastBefore = await readingsReader.SliceOfLast(lastInMonth[0].Period.AddTicks(-1),
                                q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == zone),
                                cancellationToken);
                            startVal = lastBefore.Count > 0 ? lastBefore[0].Details.Reading : 0m;
                        }

                        monthly[m - 1] = Math.Max(0m, lastInMonth[0].Details.Reading - startVal);
                    }
                }

                result.Add(new MeterConsumptionReportLine
                {
                    MeterId = meter.Id,
                    MeterName = meter.Name,
                    Zone = zone,
                    Unit = unit,
                    Month01 = monthly[0],
                    Month02 = monthly[1],
                    Month03 = monthly[2],
                    Month04 = monthly[3],
                    Month05 = monthly[4],
                    Month06 = monthly[5],
                    Month07 = monthly[6],
                    Month08 = monthly[7],
                    Month09 = monthly[8],
                    Month10 = monthly[9],
                    Month11 = monthly[10],
                    Month12 = monthly[11],
                    Total = monthly.Sum(),
                });
            }
        }

        return result.OrderBy(r => r.MeterName).ThenBy(r => r.Zone).ToList();
    }

    public async Task<List<MeterReadingsHistoryReportLine>> GetMeterReadingsHistoryAsync(
        Guid propertyId,
        Guid? meterId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var meters = new List<Meter>();
        await foreach (var m in metersRepo.GetAllAsync(q => q.Where(m => m.PropertyId == propertyId && !m.IsDeleted)))
        {
            if (!meterId.HasValue || meterId.Value == Guid.Empty || m.Id == meterId.Value)
            {
                meters.Add(m);
            }
        }

        var meterMap = meters.ToDictionary(m => m.Id);
        var meterIds = meterMap.Keys.ToHashSet();

        var allServices = new List<ServiceNode>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allServices.Add(s);
        }
        var serviceMap = allServices.ToDictionary(s => s.Id);

        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.MaxValue;

        var records = await readingsReader.Records(from, to,
            q => q.Where(r => meterIds.Contains(r.Dimensions.MeterId)), cancellationToken);

        var result = new List<MeterReadingsHistoryReportLine>();
        foreach (var rec in records)
        {
            if (!meterMap.TryGetValue(rec.Dimensions.MeterId, out var meter))
                continue;

            var unit = string.Empty;
            if (serviceMap.TryGetValue(meter.ServiceId, out var sNode) && sNode is Service s)
            {
                unit = s.Unit ?? string.Empty;
            }

            result.Add(new MeterReadingsHistoryReportLine
            {
                Date = rec.Period,
                MeterId = meter.Id,
                MeterName = meter.Name,
                Zone = rec.Dimensions.Zone,
                Reading = rec.Details.Reading,
                Unit = unit,
            });
        }

        return result.OrderByDescending(r => r.Date).ThenBy(r => r.MeterName).ThenBy(r => r.Zone).ToList();
    }
}
