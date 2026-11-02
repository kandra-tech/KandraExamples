using System.Globalization;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Constants;
using Kandra.Application.Abstractions.Dashboards;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Forms.Dashboards;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomeUtilities.Application.Common;
using HomeUtilities.Application.Constants;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dashboards;
using Microsoft.Extensions.Localization;

namespace HomeUtilities.Application.Dashboards;

[KandraDashboardBehavior(typeof(HomeDashboardDto))]
public class HomeDashboardBehavior(
    IBalanceReader<SettlementMovement, SettlementBalance> settlementsReader,
    ITurnoverReader<UtilityCostMovement> costsReader,
    IInfoRegisterReader<MeterReadingRecord> meterReadingsReader,
    IEntityRepository<Meter> metersRepo,
    IEntityRepository<ServiceNode> servicesRepo,
    IEntityRepository<Provider> providersRepo,
    IEntityRepository<Property> propertiesRepo,
    IConstantsService constants,
    ICurrentTimeZone timeZone,
    IStringLocalizer localizer,
    TimeProvider? time = null)
    : IDashboardBehavior<HomeDashboardDto, HomeDashboardResultDto>
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public string ObjectName => "HomeDashboard";

    public async Task OnNewAsync(HomeDashboardDto filters, CancellationToken cancellationToken)
    {
        var services = new List<Service>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.OfType<Service>().Where(x => !x.IsDeleted)))
        {
            if (s is Service serv)
            {
                services.Add(serv);
            }
        }

        var defaultService = services
            .Where(s => s.Billing == BillingType.Metered)
            .OrderBy(s => s.Code)
            .FirstOrDefault()
            ?? services.OrderBy(s => s.Code).FirstOrDefault();

        if (defaultService != null)
        {
            filters.ServiceId = defaultService.Id;
        }
    }

    public async Task<HomeDashboardResultDto> GetDataAsync(
        HomeDashboardDto filters,
        ILookupRepository lookup,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        // 1. Total Debt KPI & Provider Debts table
        var allBalances = await settlementsReader.Balances(q => q, cancellationToken);
        var balancesByProvider = allBalances.GroupBy(b => b.Dimensions.ProviderId);

        var allProviders = new List<Provider>();
        await foreach (var p in providersRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allProviders.Add(p);
        }
        var providerMap = allProviders.ToDictionary(p => p.Id);

        var providerDebtRows = new List<ProviderDebtRow>();
        var totalDebt = 0m;

        foreach (var p in allProviders)
        {
            var providerBalances = allBalances.Where(b => b.Dimensions.ProviderId == p.Id).ToList();
            var netDebt = providerBalances.Sum(b => b.Resources.Amount);

            totalDebt += netDebt;

            if (netDebt > 0m)
            {
                providerDebtRows.Add(new ProviderDebtRow
                {
                    ProviderId = p.Id,
                    ProviderName = p.Name,
                    Debt = netDebt,
                    Prepayment = 0m,
                });
            }
            else if (netDebt < 0m)
            {
                providerDebtRows.Add(new ProviderDebtRow
                {
                    ProviderId = p.Id,
                    ProviderName = p.Name,
                    Debt = 0m,
                    Prepayment = -netDebt,
                });
            }
            else
            {
                providerDebtRows.Add(new ProviderDebtRow
                {
                    ProviderId = p.Id,
                    ProviderName = p.Name,
                    Debt = 0m,
                    Prepayment = 0m,
                });
            }
        }

        var totalDebtKpi = new KpiData
        {
            Value = totalDebt,
            Trend = KpiTrend.LowerIsBetter,
        };

        // 2. Invoices of last month vs same month a year ago (from UtilityCosts turnover register)
        var localToday = TimeZoneHelper.ToLocalDate(now, timeZone.TimeZone);
        var lastMonthDate = new DateTime(localToday.Year, localToday.Month, 1).AddMonths(-1);
        var lastMonthStart = new DateTime(lastMonthDate.Year, lastMonthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthEnd = lastMonthStart.AddMonths(1).AddTicks(-1);
        var yearAgoStart = lastMonthStart.AddYears(-1);
        var yearAgoEnd = yearAgoStart.AddMonths(1).AddTicks(-1);

        var lastMonthMovements = await costsReader.Movements(lastMonthStart, lastMonthEnd, q => q, cancellationToken);
        var yearAgoMovements = await costsReader.Movements(yearAgoStart, yearAgoEnd, q => q, cancellationToken);

        var lastMonthInvoices = lastMonthMovements.Sum(m => m.Resources.Amount);
        var yearAgoInvoices = yearAgoMovements.Sum(m => m.Resources.Amount);

        var lastMonthKpi = new KpiData
        {
            Value = lastMonthInvoices,
            Previous = yearAgoInvoices,
            Trend = KpiTrend.LowerIsBetter,
        };

        // 3. Meters missing reading & Meter Alerts table
        var allMeters = new List<Meter>();
        await foreach (var m in metersRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allMeters.Add(m);
        }

        var allProperties = new List<Property>();
        await foreach (var p in propertiesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allProperties.Add(p);
        }
        var propertyMap = allProperties.ToDictionary(p => p.Id);

        var thisMonthLocalStart = new DateTime(localToday.Year, localToday.Month, 1);
        var (thisMonthStartUtc, _) = TimeZoneHelper.LocalMonthBoundsUtc(thisMonthLocalStart, timeZone.TimeZone);
        var expectedDay = (await constants.GetAsync<ExpectedReadingsDayConstant>(localToday, cancellationToken))?.Value ?? 25;
        var dueDay = Math.Clamp(expectedDay, 1, DateTime.DaysInMonth(localToday.Year, localToday.Month));
        var expectedReadingsDueDate = new DateTime(localToday.Year, localToday.Month, dueDay);

        var recentReadings = await meterReadingsReader.Records(thisMonthStartUtc, now, null, cancellationToken);
        var metersWithReadingThisMonth = recentReadings.Select(r => r.Dimensions.MeterId).ToHashSet();

        var missingReadingsCount = 0;
        var meterAlertRows = new List<MeterAlertRow>();

        var verificationCutoff = localToday.AddDays(60);

        foreach (var meter in allMeters)
        {
            propertyMap.TryGetValue(meter.PropertyId, out var prop);
            var propName = prop?.Name ?? string.Empty;

            if (!metersWithReadingThisMonth.Contains(meter.Id))
            {
                missingReadingsCount++;
                meterAlertRows.Add(new MeterAlertRow
                {
                    MeterId = meter.Id,
                    PropertyName = propName,
                    MeterName = meter.Name,
                    Issue = localizer["Alert_MissingReading"].Value,
                    DueDate = expectedReadingsDueDate,
                });
            }

            if (meter.NextVerificationDate <= verificationCutoff)
            {
                meterAlertRows.Add(new MeterAlertRow
                {
                    MeterId = meter.Id,
                    PropertyName = propName,
                    MeterName = meter.Name,
                    Issue = localizer["Alert_VerificationDue"].Value,
                    DueDate = meter.NextVerificationDate,
                });
            }
        }

        var missingReadingsKpi = new KpiData
        {
            Value = missingReadingsCount,
            Trend = KpiTrend.LowerIsBetter,
        };

        // 4. Costs per month by service group (last 12 months, stacked bar chart)
        var months = DashboardPeriods.Last(12, DashboardBucketSize.Month, timeZone.TimeZone, now);
        var costMovements = await costsReader.Movements(months[0].StartUtc, months[^1].EndUtc.AddTicks(-1),
            q => q, cancellationToken);

        var allServiceNodes = new List<ServiceNode>();
        await foreach (var s in servicesRepo.GetAllAsync(q => q.Where(x => !x.IsDeleted)))
        {
            allServiceNodes.Add(s);
        }
        var serviceNodeMap = allServiceNodes.ToDictionary(s => s.Id);

        var groupsWithBuckets = new Dictionary<string, decimal[]>();

        foreach (var mov in costMovements)
        {
            var idx = DashboardPeriods.IndexOf(months, mov.Period);
            if (idx < 0 || idx >= 12) continue;

            var groupName = localizer["ServiceGroup_Other"].Value;
            if (serviceNodeMap.TryGetValue(mov.Dimensions.ServiceId, out var sn) && sn is Service s && s.ParentId.HasValue)
            {
                if (serviceNodeMap.TryGetValue(s.ParentId.Value, out var parentNode))
                {
                    groupName = parentNode.Name;
                }
            }

            if (!groupsWithBuckets.TryGetValue(groupName, out var bucketArr))
            {
                bucketArr = new decimal[12];
                groupsWithBuckets[groupName] = bucketArr;
            }
            bucketArr[idx] += mov.Resources.Amount;
        }

        var monthLabels = months.Select(m => m.Start.ToString("MMM yy", CultureInfo.CurrentUICulture)).ToList();
        var costsChart = new CategoryChartData
        {
            Labels = monthLabels,
        };

        foreach (var (grp, arr) in groupsWithBuckets)
        {
            costsChart.AddSeries(grp, arr);
        }

        // 5. Chosen service's consumption per month (this year and last year, 2 series line chart)
        var chosenServiceId = filters.ServiceId;
        if (chosenServiceId == Guid.Empty && allServiceNodes.OfType<Service>().Any())
        {
            chosenServiceId = allServiceNodes.OfType<Service>().First().Id;
        }

        var thisYearStart = new DateTime(localToday.Year, 1, 1);
        var thisYearEnd = new DateTime(localToday.Year, 12, 31, 23, 59, 59);
        var lastYearStart = new DateTime(localToday.Year - 1, 1, 1);
        var lastYearEnd = new DateTime(localToday.Year - 1, 12, 31, 23, 59, 59);

        var thisYearCosts = await costsReader.Movements(thisYearStart, thisYearEnd,
            q => q.Where(m => m.Dimensions.ServiceId == chosenServiceId), cancellationToken);

        var lastYearCosts = await costsReader.Movements(lastYearStart, lastYearEnd,
            q => q.Where(m => m.Dimensions.ServiceId == chosenServiceId), cancellationToken);

        var thisYearMonthly = new decimal[12];
        foreach (var m in thisYearCosts)
        {
            var mIdx = m.Period.Month - 1;
            if (mIdx >= 0 && mIdx < 12) thisYearMonthly[mIdx] += m.Resources.Quantity;
        }

        var lastYearMonthly = new decimal[12];
        foreach (var m in lastYearCosts)
        {
            var mIdx = m.Period.Month - 1;
            if (mIdx >= 0 && mIdx < 12) lastYearMonthly[mIdx] += m.Resources.Quantity;
        }

        var calendarMonthLabels = Enumerable.Range(1, 12)
            .Select(i => new DateTime(2000, i, 1).ToString("MMM", CultureInfo.CurrentUICulture))
            .ToList();

        var consumptionChart = new CategoryChartData
        {
            Labels = calendarMonthLabels,
        };
        consumptionChart.AddSeries(localToday.Year.ToString(), thisYearMonthly);
        consumptionChart.AddSeries((localToday.Year - 1).ToString(), lastYearMonthly);

        return new HomeDashboardResultDto
        {
            Filters = filters,
            TotalDebtKpi = totalDebtKpi,
            LastMonthInvoicesKpi = lastMonthKpi,
            MissingReadingsKpi = missingReadingsKpi,
            CostsByServiceGroup = costsChart,
            ServiceConsumption = consumptionChart,
            ProviderDebts = new TableData<ProviderDebtRow> { Rows = providerDebtRows },
            MeterAlerts = new TableData<MeterAlertRow> { Rows = meterAlertRows },
        };
    }
}
