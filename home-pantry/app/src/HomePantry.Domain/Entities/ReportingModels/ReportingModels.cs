using HomePantry.Enums;

namespace HomePantry.Domain.Entities.ReportingModels;

public sealed class StockOverviewLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public ProductUnit ProductUnit { get; set; }
    public string Unit { get; set; } = "";
    public Guid? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string? LocationName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public DateTime? NearestBestBeforeDate { get; set; }
    public decimal MinimumStock { get; set; }
    public bool BelowMinimum { get; set; }
}

public sealed class ExpiringLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = "";
    public string LocationName { get; set; } = "";
    public DateTime BestBeforeDate { get; set; }
    public decimal Quantity { get; set; }
    public int DaysLeft { get; set; }
    public bool IsExpired { get; set; }
}

public sealed class ShoppingNeedsLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal OnHandQuantity { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal ShortfallQuantity { get; set; }
    public decimal RecipeNeededQuantity { get; set; }
    public decimal RecipeAdditionalShortfall { get; set; }
    public decimal TotalNeededQuantity { get; set; }
    public decimal LastPrice { get; set; }
    public decimal EstimatedCost { get; set; }
}

public sealed record MissingIngredientItem(string ProductName, decimal Needed, decimal OnHand);

public sealed class CanICookItLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }
    public string RecipeCode { get; set; } = "";
    public string RecipeName { get; set; } = "";
    public decimal Servings { get; set; }
    public bool CanCook { get; set; }
    public IReadOnlyList<MissingIngredientItem> MissingItems { get; set; } = [];
    public string MissingIngredients { get; set; } = "";
}

public sealed class SpendingLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductGroupId { get; set; }
    public string ProductGroupCode { get; set; } = "";
    public string ProductGroupName { get; set; } = "";
    public Guid StoreId { get; set; }
    public string StoreCode { get; set; } = "";
    public string StoreName { get; set; } = "";
    public decimal PurchasedAmount { get; set; }
}

public sealed class ChoresReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChoreId { get; set; }
    public string ChoreCode { get; set; } = "";
    public string ChoreName { get; set; } = "";
    public ChoreRecurrence Recurrence { get; set; }
    public DateTime? LastDoneDate { get; set; }
    public string? DoneByName { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class BatteriesReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatteryId { get; set; }
    public string BatteryCode { get; set; } = "";
    public string BatteryName { get; set; } = "";
    public string UsedIn { get; set; } = "";
    public BatteryKind BatteryKind { get; set; }
    public int ChargeIntervalDays { get; set; }
    public DateTime? LastChargeDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsOverdue { get; set; }
    public decimal TotalCycles { get; set; }
}

public sealed class EquipmentReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = "";
    public DateTime? BoughtOn { get; set; }
    public DateTime? WarrantyUntil { get; set; }
    public int? DaysLeft { get; set; }
    public bool HasManual { get; set; }
    public bool HasPhoto { get; set; }
}

public sealed class WeekPlanReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public MealPlanDay Day { get; set; }
    public Guid RecipeId { get; set; }
    public string RecipeCode { get; set; } = "";
    public string RecipeName { get; set; } = "";
    public decimal Servings { get; set; }
    public bool CanCook { get; set; }
    public string MissingIngredients { get; set; } = "";
}

public sealed class CalendarReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = "";
    public string ChoresDue { get; set; } = "";
    public string BatteriesDue { get; set; } = "";
    public string PlannedMeals { get; set; } = "";
}

public sealed class TodayScheduleItem
{
    public bool IsMeal { get; set; }
    public string ItemName { get; set; } = "";
    public string? DoneByName { get; set; }
    public bool IsOverdue { get; set; }
    public decimal Servings { get; set; }
}
