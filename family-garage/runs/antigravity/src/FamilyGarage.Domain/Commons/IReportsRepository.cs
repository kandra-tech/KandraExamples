using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.ReportingModels;
using FamilyGarage.Enums;

namespace FamilyGarage.Domain.Commons;

public interface IReportsRepository
{
    Task<FinanceContract?> GetFinanceContractAsync(Guid id, CancellationToken ct = default);
    Task<Dictionary<Guid, (int Km, int Months)>> GetWorkItemIntervalsAsync(IEnumerable<Guid> itemIds, CancellationToken ct = default);
    Task<Accident?> GetAccidentAsync(Guid id, CancellationToken ct = default);

    // Every date argument is a UTC instant; "today" and months are the user's, so the callers pass the user's time zone.
    // A period's "to" includes its whole local day.
    Task<List<UpcomingItem>> GetUpcomingItemsAsync(int lookAheadDays, int lookAheadKm, Guid? vehicleId, DateOnly today, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<List<TcoReportLine>> GetTotalCostOfOwnershipAsync(DateTime? from, DateTime? to, Guid? vehicleId, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<List<CostsPivotLine>> GetCostsPivotAsync(DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<List<FinancingReportLine>> GetFinancingReportAsync(DateTime asOf, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<List<VehicleHistoryReportLine>> GetVehicleHistoryAsync(Guid vehicleId, DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<(List<FuelConsumptionReportLine> Lines, decimal AverageConsumption)> GetFuelConsumptionAsync(Guid vehicleId, DateTime? from, DateTime? to, TimeZoneInfo? zone, CancellationToken ct = default);

    /// <summary>Costs net of sales from <paramref name="fromUtc"/> (inclusive) to <paramref name="toUtc"/> (exclusive).</summary>
    Task<decimal> GetCostsForPeriodAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<decimal> GetTotalDebtAsync(CancellationToken ct = default);
    Task<decimal> GetAverageFuelConsumptionAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<Dictionary<string, Dictionary<CostCategory, decimal>>> GetMonthlyCostsByCategoryAsync(DateTime fromUtc, DateTime toUtc, TimeZoneInfo? zone, CancellationToken ct = default);
    Task<List<VehicleDashboardStatus>> GetVehicleDashboardStatusAsync(DateOnly today, TimeZoneInfo? zone, CancellationToken ct = default);
}
