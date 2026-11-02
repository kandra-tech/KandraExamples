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
