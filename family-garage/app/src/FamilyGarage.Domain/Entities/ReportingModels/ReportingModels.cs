using FamilyGarage.Enums;

namespace FamilyGarage.Domain.Entities.ReportingModels;

/// <summary>
/// One due event. <see cref="Subject"/> is the work item's name or the contract's number (data, not text); a policy
/// event has none and carries its <see cref="PolicyKind"/>, which the caller turns into the user's language.
/// <see cref="Key"/> identifies this occurrence (it includes the due date or kilometres), so the reminder for the next
/// occurrence of the same job is a new event.
/// </summary>
public record UpcomingItem(
    Guid VehicleId,
    string VehicleName,
    UpcomingEventKind Kind,
    string? Subject,
    PolicyKind? PolicyKind,
    DateOnly? DueDate,
    int? DueKm,
    int? RemainingDays,
    int? RemainingKm,
    bool IsOverdue,
    string Key);

public record VehicleDashboardStatus(
    Guid VehicleId,
    string VehicleName,
    int Odometer,
    decimal Tco,
    decimal DebtOwed,
    UpcomingItem? NextDue);

public record TcoReportLine(
    Guid VehicleId,
    string VehicleName,
    decimal PurchasePrice,
    decimal Fuel,
    decimal Maintenance,
    decimal Repair,
    decimal Insurance,
    decimal Tax,
    decimal InterestAndFees,
    decimal Other,
    decimal SalePrice,
    decimal NetCost,
    int KmDriven,
    decimal CostPerKm,
    decimal CostPerMonth,
    decimal DebtOwed);

public record CostsPivotLine(
    string VehicleCode,
    string VehicleName,
    string Month,
    decimal Amount);

public record FinancingReportLine(
    string ContractNumber,
    string VehicleName,
    string LenderName,
    decimal FinancedAmount,
    decimal PrincipalPaid,
    decimal InterestAndFeesPaid,
    decimal DebtOwed,
    int PaymentsMade,
    int MonthsLeft);

public record VehicleHistoryReportLine(
    DateTime Date,
    int Odometer,
    string ItemName,
    decimal Quantity,
    decimal Amount,
    string DocumentCode);

public record FuelConsumptionReportLine(
    DateTime Date,
    int Odometer,
    decimal Litres,
    int Distance,
    decimal Consumption);
