using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Registers;
using FamilyGarage.Domain.Entities.ReportingModels;
using FamilyGarage.Domain.Reporting;
using FamilyGarage.Persistence.Contexts;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Persistence.Repositories;

internal class FamilyGarageReportsRepository(FamilyGarageDbContext dbContext) : IReportsRepository
{
    public Task<FinanceContract?> GetFinanceContractAsync(Guid id, CancellationToken ct = default)
    {
        return dbContext.Set<FinanceContract>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Dictionary<Guid, (int Km, int Months)>> GetWorkItemIntervalsAsync(IEnumerable<Guid> itemIds, CancellationToken ct = default)
    {
        var idList = itemIds.Distinct().ToList();
        var items = await dbContext.Set<ItemNode>()
            .OfType<Item>()
            .AsNoTracking()
            .Where(i => idList.Contains(i.Id) && i.Kind == ItemKind.Work && (i.ServiceIntervalKm > 0 || i.ServiceIntervalMonths > 0))
            .Select(i => new { i.Id, i.ServiceIntervalKm, i.ServiceIntervalMonths })
            .ToListAsync(ct);

        return items.ToDictionary(i => i.Id, i => (i.ServiceIntervalKm, i.ServiceIntervalMonths));
    }

    public Task<Accident?> GetAccidentAsync(Guid id, CancellationToken ct = default)
    {
        return dbContext.Set<Accident>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<UpcomingItem>> GetUpcomingItemsAsync(int lookAheadDays, int lookAheadKm, Guid? vehicleId, DateOnly today, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var vehicleQuery = dbContext.Set<Vehicle>().AsNoTracking();
        if (vehicleId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(v => v.Id == vehicleId.Value);
        }
        var vehicles = await vehicleQuery.ToListAsync(ct);
        var results = new List<UpcomingItem>();

        var workItems = await dbContext.Set<ItemNode>().OfType<Item>().AsNoTracking()
            .Where(i => i.Kind == ItemKind.Work && (i.ServiceIntervalKm > 0 || i.ServiceIntervalMonths > 0))
            .ToListAsync(ct);

        foreach (var v in vehicles)
        {
            var lastOdoRecord = await dbContext.Set<OdometerRecord>().AsNoTracking()
                .Where(o => o.Active && o.Dimensions.VehicleId == v.Id)
                .OrderByDescending(o => o.Period).ThenByDescending(o => o.Id)
                .FirstOrDefaultAsync(ct);
            int currentOdo = lastOdoRecord?.Details.Kilometres ?? 0;

            var purchases = await dbContext.Set<VehiclePurchase>().AsNoTracking()
                .Where(p => p.IsActive && p.VehicleId == v.Id)
                .OrderByDescending(p => p.Date)
                .ToListAsync(ct);
            var mainPurchase = purchases.FirstOrDefault();
            var baseDate = LocalCalendar.LocalDate(mainPurchase?.Date ?? v.Created, zone);
            var baseKm = mainPurchase?.Odometer ?? 0;

            foreach (var item in workItems)
            {
                var lastDone = await dbContext.Set<LastDoneRecord>().AsNoTracking()
                    .Where(l => l.Active && l.Dimensions.VehicleId == v.Id && l.Dimensions.ItemId == item.Id)
                    .OrderByDescending(l => l.Period)
                    .FirstOrDefaultAsync(ct);

                var itemBaseDate = lastDone is not null ? LocalCalendar.LocalDate(lastDone.Details.Date, zone) : baseDate;
                var itemBaseKm = lastDone?.Details.Odometer ?? baseKm;

                int? dueKm = item.ServiceIntervalKm > 0 ? itemBaseKm + item.ServiceIntervalKm : null;
                DateOnly? dueDate = item.ServiceIntervalMonths > 0 ? itemBaseDate.AddMonths(item.ServiceIntervalMonths) : null;

                int? remainingKm = dueKm.HasValue ? dueKm.Value - currentOdo : null;
                int? remainingDays = dueDate.HasValue ? dueDate.Value.DayNumber - today.DayNumber : null;

                bool isOverdue = (remainingKm.HasValue && remainingKm.Value < 0) || (remainingDays.HasValue && remainingDays.Value < 0);
                bool isDue = isOverdue ||
                             (remainingKm.HasValue && remainingKm.Value <= lookAheadKm) ||
                             (remainingDays.HasValue && remainingDays.Value <= lookAheadDays);

                if (isDue)
                {
                    // One occurrence per "last done" (or the purchase, when it was never done): the next service starts a new one.
                    results.Add(new UpcomingItem(
                        v.Id,
                        v.Name,
                        UpcomingEventKind.Maintenance,
                        item.Name,
                        null,
                        dueDate,
                        dueKm,
                        remainingDays,
                        remainingKm,
                        isOverdue,
                        $"work:{item.Id}:{itemBaseKm}:{itemBaseDate:yyyyMMdd}"));
                }
            }

            var policyRecords = await dbContext.Set<PolicyValidityRecord>().AsNoTracking()
                .Where(p => p.Active && p.Dimensions.VehicleId == v.Id)
                .ToListAsync(ct);

            var latestPolicies = policyRecords
                .GroupBy(p => p.Dimensions.PolicyKind)
                .Select(g => g.OrderByDescending(x => x.Period).First())
                .ToList();

            foreach (var policy in latestPolicies)
            {
                // ValidTo is a calendar date (the policy's DateOnly), not an instant.
                var validTo = DateOnly.FromDateTime(policy.Details.ValidTo);
                int remainingDays = validTo.DayNumber - today.DayNumber;
                bool isOverdue = remainingDays < 0;
                bool isDue = isOverdue || remainingDays <= lookAheadDays;

                if (isDue)
                {
                    results.Add(new UpcomingItem(
                        v.Id,
                        v.Name,
                        UpcomingEventKind.Policy,
                        null,
                        policy.Dimensions.PolicyKind,
                        validTo,
                        null,
                        remainingDays,
                        null,
                        isOverdue,
                        $"policy:{policy.Dimensions.PolicyKind}:{validTo:yyyyMMdd}"));
                }
            }

            var contracts = await dbContext.Set<FinanceContract>().AsNoTracking()
                .Where(c => c.VehicleId == v.Id)
                .ToListAsync(ct);

            foreach (var contract in contracts)
            {
                var debtBal = await dbContext.Set<FinanceDebtBalance>().AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Dimensions.ContractId == contract.Id, ct);
                var debt = debtBal?.Resources.Amount ?? 0m;
                if (debt > 0)
                {
                    var lastPayment = await dbContext.Set<FinancePayment>().AsNoTracking()
                        .Where(p => p.IsActive && p.FinanceContractId == contract.Id)
                        .OrderByDescending(p => p.Date)
                        .FirstOrDefaultAsync(ct);
                    var financedPurchase = purchases.FirstOrDefault(p => p.FinanceContractId == contract.Id);

                    var lastPaymentDate = lastPayment is not null
                        ? LocalCalendar.LocalDate(lastPayment.Date, zone)
                        : financedPurchase is not null ? LocalCalendar.LocalDate(financedPurchase.Date, zone) : baseDate;
                    var nextMonth = lastPaymentDate.AddMonths(1);
                    var day = Math.Min(contract.PaymentDayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    var nextDueDate = new DateOnly(nextMonth.Year, nextMonth.Month, day);

                    int remainingDays = nextDueDate.DayNumber - today.DayNumber;
                    bool isOverdue = remainingDays < 0;
                    bool isDue = isOverdue || remainingDays <= lookAheadDays;

                    if (isDue)
                    {
                        results.Add(new UpcomingItem(
                            v.Id,
                            v.Name,
                            UpcomingEventKind.Payment,
                            contract.Number,
                            null,
                            nextDueDate,
                            null,
                            remainingDays,
                            null,
                            isOverdue,
                            $"payment:{contract.Id}:{nextDueDate:yyyyMM}"));
                    }
                }
            }
        }

        return results
            .OrderBy(r => !r.IsOverdue)
            .ThenBy(r => r.RemainingDays ?? int.MaxValue)
            .ThenBy(r => r.RemainingKm ?? int.MaxValue)
            .ToList();
    }

    public async Task<List<TcoReportLine>> GetTotalCostOfOwnershipAsync(DateTime? from, DateTime? to, Guid? vehicleId, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var vehicleQuery = dbContext.Set<Vehicle>().AsNoTracking();
        if (vehicleId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(v => v.Id == vehicleId.Value);
        }
        var vehicles = await vehicleQuery.ToListAsync(ct);
        DateTime? toEnd = to.HasValue ? LocalCalendar.EndOfLocalDayUtc(to.Value, zone) : null;

        var costsQuery = dbContext.Set<VehicleCostsMovement>().AsNoTracking().Where(c => c.Active);
        if (from.HasValue) costsQuery = costsQuery.Where(c => c.Period >= from.Value);
        if (toEnd.HasValue) costsQuery = costsQuery.Where(c => c.Period < toEnd.Value);
        var costs = await costsQuery.ToListAsync(ct);

        // Every reading up to the end of the period: the kilometres driven run from the last reading before the period
        // (or the first one inside it) to the last one inside it.
        var odoQuery = dbContext.Set<OdometerRecord>().AsNoTracking().Where(o => o.Active);
        if (toEnd.HasValue) odoQuery = odoQuery.Where(o => o.Period < toEnd.Value);
        var odometers = await odoQuery.ToListAsync(ct);

        // The debt still owed at the end of the period, not today.
        var contracts = await dbContext.Set<FinanceContract>().AsNoTracking().ToListAsync(ct);
        var debtQuery = dbContext.Set<FinanceDebtMovement>().AsNoTracking().Where(d => d.Active);
        if (toEnd.HasValue) debtQuery = debtQuery.Where(d => d.Period < toEnd.Value);
        var debtMovements = await debtQuery.ToListAsync(ct);

        var today = LocalCalendar.LocalDate(DateTime.UtcNow, zone);
        var startDate = from.HasValue
            ? LocalCalendar.LocalDate(from.Value, zone)
            : costs.Count > 0 ? LocalCalendar.LocalDate(costs.Min(c => c.Period), zone) : today;
        var endDate = to.HasValue ? LocalCalendar.LocalDate(to.Value, zone) : today;
        var monthsCount = Math.Max(1, ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month + 1);

        var lines = new List<TcoReportLine>();
        foreach (var v in vehicles)
        {
            var vCosts = costs.Where(c => c.Dimensions.VehicleId == v.Id).ToList();

            var purchasePrice = vCosts.Where(c => c.Dimensions.Category == CostCategory.Purchase && !c.IsExpense).Sum(c => c.Resources.Amount);
            var salePrice = vCosts.Where(c => c.Dimensions.Category == CostCategory.Purchase && c.IsExpense).Sum(c => c.Resources.Amount);

            var fuel = vCosts.Where(c => c.Dimensions.Category == CostCategory.Fuel && !c.IsExpense).Sum(c => c.Resources.Amount);
            var maintenance = vCosts.Where(c => c.Dimensions.Category == CostCategory.Maintenance && !c.IsExpense).Sum(c => c.Resources.Amount);
            var repair = vCosts.Where(c => c.Dimensions.Category == CostCategory.Repair && !c.IsExpense).Sum(c => c.Resources.Amount);
            var insurance = vCosts.Where(c => c.Dimensions.Category == CostCategory.Insurance && !c.IsExpense).Sum(c => c.Resources.Amount);
            var tax = vCosts.Where(c => c.Dimensions.Category == CostCategory.Tax && !c.IsExpense).Sum(c => c.Resources.Amount);
            var interest = vCosts.Where(c => c.Dimensions.Category == CostCategory.InterestAndFees && !c.IsExpense).Sum(c => c.Resources.Amount);
            var other = vCosts.Where(c => c.Dimensions.Category == CostCategory.Other && !c.IsExpense).Sum(c => c.Resources.Amount);

            var netCost = purchasePrice + fuel + maintenance + repair + insurance + tax + interest + other - salePrice;

            var vOdo = odometers.Where(o => o.Dimensions.VehicleId == v.Id).OrderBy(o => o.Period).ThenBy(o => o.Details.Kilometres).ToList();
            var inPeriod = from.HasValue ? vOdo.Where(o => o.Period >= from.Value).ToList() : vOdo;
            var before = from.HasValue ? vOdo.LastOrDefault(o => o.Period < from.Value) : null;
            var startReading = before ?? inPeriod.FirstOrDefault();
            var endReading = inPeriod.LastOrDefault();
            int kmDriven = startReading is not null && endReading is not null
                ? Math.Max(0, endReading.Details.Kilometres - startReading.Details.Kilometres)
                : 0;

            decimal costPerKm = kmDriven > 0 ? Math.Round(netCost / kmDriven, 2) : 0m;
            decimal costPerMonth = Math.Round(netCost / monthsCount, 2);

            var vContractIds = contracts.Where(c => c.VehicleId == v.Id).Select(c => c.Id).ToHashSet();
            decimal debtOwed = debtMovements
                .Where(d => vContractIds.Contains(d.Dimensions.ContractId))
                .Sum(d => d.IsExpense ? -d.Resources.Amount : d.Resources.Amount);

            lines.Add(new TcoReportLine(
                v.Id,
                v.Name,
                purchasePrice,
                fuel,
                maintenance,
                repair,
                insurance,
                tax,
                interest,
                other,
                salePrice,
                netCost,
                kmDriven,
                costPerKm,
                costPerMonth,
                debtOwed));
        }

        return lines;
    }

    public async Task<List<CostsPivotLine>> GetCostsPivotAsync(DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var costsQuery = dbContext.Set<VehicleCostsMovement>().AsNoTracking().Where(c => c.Active);
        if (from.HasValue) costsQuery = costsQuery.Where(c => c.Period >= from.Value);
        if (to.HasValue)
        {
            var toEnd = LocalCalendar.EndOfLocalDayUtc(to.Value, zone);
            costsQuery = costsQuery.Where(c => c.Period < toEnd);
        }
        var costs = await costsQuery.ToListAsync(ct);

        var vehicles = await dbContext.Set<Vehicle>().AsNoTracking().ToDictionaryAsync(v => v.Id, ct);

        return costs
            .GroupBy(c => new { c.Dimensions.VehicleId, Month = LocalCalendar.MonthKey(c.Period, zone) })
            .Select(g =>
            {
                var vehicle = vehicles.GetValueOrDefault(g.Key.VehicleId);
                var amount = g.Sum(x => x.IsExpense ? -x.Resources.Amount : x.Resources.Amount);
                return new CostsPivotLine(
                    vehicle?.Code ?? g.Key.VehicleId.ToString(),
                    vehicle?.Name ?? string.Empty,
                    g.Key.Month,
                    amount);
            })
            .OrderBy(l => l.VehicleName)
            .ThenBy(l => l.Month)
            .ToList();
    }

    public async Task<List<FinancingReportLine>> GetFinancingReportAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var asOfEnd = LocalCalendar.EndOfLocalDayUtc(asOf, zone);
        var contracts = await dbContext.Set<FinanceContract>().AsNoTracking().ToListAsync(ct);
        var vehicles = await dbContext.Set<Vehicle>().AsNoTracking().ToDictionaryAsync(v => v.Id, ct);
        var providers = await dbContext.Set<Provider>().AsNoTracking().ToDictionaryAsync(p => p.Id, ct);

        // The financed amount, the principal paid and the debt come from the debt register as of the date. Interest, fees
        // and the payment count are in no register by contract, so they come from the posted payments.
        var debtMovements = await dbContext.Set<FinanceDebtMovement>().AsNoTracking()
            .Where(d => d.Active && d.Period < asOfEnd)
            .ToListAsync(ct);
        var payments = await dbContext.Set<FinancePayment>().AsNoTracking()
            .Where(p => p.IsActive && p.Date < asOfEnd)
            .ToListAsync(ct);

        var lines = new List<FinancingReportLine>();
        foreach (var contract in contracts)
        {
            var vehicle = vehicles.GetValueOrDefault(contract.VehicleId);
            var lender = providers.GetValueOrDefault(contract.LenderId);

            var cMovements = debtMovements.Where(d => d.Dimensions.ContractId == contract.Id).ToList();
            decimal financedAmount = cMovements.Where(d => !d.IsExpense).Sum(d => d.Resources.Amount);
            decimal principalPaid = cMovements.Where(d => d.IsExpense).Sum(d => d.Resources.Amount);
            decimal debtOwed = financedAmount - principalPaid;

            var cPayments = payments.Where(p => p.FinanceContractId == contract.Id).ToList();
            decimal interestPaid = cPayments.Sum(p => p.Interest + p.Fees);
            int paymentsMade = cPayments.Count;
            int monthsLeft = Math.Max(0, contract.TermMonths - paymentsMade);

            lines.Add(new FinancingReportLine(
                contract.Number,
                vehicle?.Name ?? string.Empty,
                lender?.Name ?? string.Empty,
                financedAmount,
                principalPaid,
                interestPaid,
                debtOwed,
                paymentsMade,
                monthsLeft));
        }

        return lines;
    }

    public async Task<List<VehicleHistoryReportLine>> GetVehicleHistoryAsync(Guid vehicleId, DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var query = dbContext.Set<ServiceLinesMovement>().AsNoTracking()
            .Where(s => s.Active && s.Dimensions.VehicleId == vehicleId);
        if (from.HasValue) query = query.Where(s => s.Period >= from.Value);
        if (to.HasValue)
        {
            var toEnd = LocalCalendar.EndOfLocalDayUtc(to.Value, zone);
            query = query.Where(s => s.Period < toEnd);
        }

        var movements = await query.OrderByDescending(s => s.Period).ThenByDescending(s => s.Id).ToListAsync(ct);
        var itemIds = movements.Select(m => m.Dimensions.ItemId).Distinct().ToList();
        var items = await dbContext.Set<ItemNode>().AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        var documentIds = movements.Select(m => m.DocumentId).Distinct().ToList();
        var services = await dbContext.Set<Service>().AsNoTracking()
            .Where(s => documentIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Code, ct);

        return movements.Select(m => new VehicleHistoryReportLine(
            m.Period,
            m.Details.Odometer,
            items.GetValueOrDefault(m.Dimensions.ItemId, string.Empty),
            m.Resources.Quantity,
            m.Resources.Amount,
            services.GetValueOrDefault(m.DocumentId, string.Empty)
        )).ToList();
    }

    public async Task<(List<FuelConsumptionReportLine> Lines, decimal AverageConsumption)> GetFuelConsumptionAsync(Guid vehicleId, DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var query = dbContext.Set<Refuel>().AsNoTracking()
            .Where(r => r.IsActive && r.VehicleId == vehicleId);
        if (from.HasValue) query = query.Where(r => r.Date >= from.Value);
        if (to.HasValue)
        {
            var toEnd = LocalCalendar.EndOfLocalDayUtc(to.Value, zone);
            query = query.Where(r => r.Date < toEnd);
        }

        var refuels = await query.ToListAsync(ct);
        var intervals = FuelConsumptionCalculator.Intervals(refuels.Select(r => new RefuelReading(r.Date, r.Odometer, r.Litres, r.FullTank)));

        var lines = intervals.Select(i => new FuelConsumptionReportLine(
            i.Refuel.Date,
            i.Refuel.Odometer,
            i.Refuel.Litres,
            i.Distance,
            i.Consumption)).ToList();

        return (lines.OrderByDescending(l => l.Date).ToList(), FuelConsumptionCalculator.Average(intervals));
    }

    public async Task<decimal> GetCostsForPeriodAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var costs = await dbContext.Set<VehicleCostsMovement>().AsNoTracking()
            .Where(c => c.Active && c.Period >= fromUtc && c.Period < toUtc)
            .ToListAsync(ct);

        return costs.Sum(c => c.IsExpense ? -c.Resources.Amount : c.Resources.Amount);
    }

    public async Task<decimal> GetTotalDebtAsync(CancellationToken ct = default)
    {
        return await dbContext.Set<FinanceDebtBalance>().AsNoTracking().SumAsync(d => d.Resources.Amount, ct);
    }

    public async Task<decimal> GetAverageFuelConsumptionAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var refuels = await dbContext.Set<Refuel>().AsNoTracking()
            .Where(r => r.IsActive && r.Date >= fromUtc && r.Date < toUtc)
            .ToListAsync(ct);

        var intervals = refuels
            .GroupBy(r => r.VehicleId)
            .SelectMany(g => FuelConsumptionCalculator.Intervals(g.Select(r => new RefuelReading(r.Date, r.Odometer, r.Litres, r.FullTank))))
            .ToList();
        return FuelConsumptionCalculator.Average(intervals);
    }

    public async Task<Dictionary<string, Dictionary<CostCategory, decimal>>> GetMonthlyCostsByCategoryAsync(DateTime fromUtc, DateTime toUtc, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var costs = await dbContext.Set<VehicleCostsMovement>().AsNoTracking()
            .Where(c => c.Active && c.Period >= fromUtc && c.Period < toUtc)
            .ToListAsync(ct);

        var result = new Dictionary<string, Dictionary<CostCategory, decimal>>();
        foreach (var group in costs.GroupBy(c => LocalCalendar.MonthKey(c.Period, zone)))
        {
            var catDict = new Dictionary<CostCategory, decimal>();
            foreach (var catGroup in group.GroupBy(c => c.Dimensions.Category))
            {
                catDict[catGroup.Key] = catGroup.Sum(x => x.IsExpense ? -x.Resources.Amount : x.Resources.Amount);
            }
            result[group.Key] = catDict;
        }

        return result;
    }

    public async Task<List<VehicleDashboardStatus>> GetVehicleDashboardStatusAsync(DateOnly today, TimeZoneInfo? zone, CancellationToken ct = default)
    {
        var vehicles = await dbContext.Set<Vehicle>().AsNoTracking().ToListAsync(ct);
        var tco = await GetTotalCostOfOwnershipAsync(null, null, null, zone, ct);
        var tcoDict = tco.ToDictionary(t => t.VehicleId);

        // The next event of each vehicle, however far ahead it is.
        var upcoming = await GetUpcomingItemsAsync(int.MaxValue, int.MaxValue, null, today, zone, ct);
        var upcomingByVehicle = upcoming.GroupBy(u => u.VehicleId).ToDictionary(g => g.Key, g => g.First());

        var result = new List<VehicleDashboardStatus>();
        foreach (var v in vehicles)
        {
            var lastOdo = await dbContext.Set<OdometerRecord>().AsNoTracking()
                .Where(o => o.Active && o.Dimensions.VehicleId == v.Id)
                .OrderByDescending(o => o.Period).ThenByDescending(o => o.Id)
                .FirstOrDefaultAsync(ct);
            int odo = lastOdo?.Details.Kilometres ?? 0;

            var vTco = tcoDict.GetValueOrDefault(v.Id);
            result.Add(new VehicleDashboardStatus(v.Id, v.Name, odo, vTco?.NetCost ?? 0m, vTco?.DebtOwed ?? 0m, upcomingByVehicle.GetValueOrDefault(v.Id)));
        }

        return result;
    }
}
