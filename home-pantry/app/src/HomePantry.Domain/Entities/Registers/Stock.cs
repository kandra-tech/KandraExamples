using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using HomePantry.Domain.Entities.Dictionaries;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class StockDimensions : IRegisterDimensions<StockDimensions>
{
    [Caption("Product")]
    [Description("The product tracked in stock.")]
    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Caption("Location")]
    [Description("The storage location where the batch is stored.")]
    public Guid LocationId { get; set; }

    [JsonIgnore]
    public LocationNode Location { get; set; } = null!;

    [Caption("Stock_BestBeforeDate")]
    [Description("The best-before date of this batch; 9999-12-31 for non-perishable goods.")]
    public DateTime BestBeforeDate { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class StockResources : IRegisterResources<StockResources>
{
    [Precision(14, 3)]
    [Caption("Quantity")]
    [Format("N3")]
    [Description("Stock quantity on hand in product's base unit. Must never be negative.")]
    public decimal Quantity { get; init; }

    [Precision(14, 2)]
    [Caption("Value")]
    [Format("N2")]
    [Description("Total monetary value of the batch in home currency (UAH). Must never be negative.")]
    public decimal Value { get; init; }

    public static StockResources AdditiveIdentity => new();

    public static StockResources operator +(StockResources a, StockResources b)
        => new() { Quantity = a.Quantity + b.Quantity, Value = a.Value + b.Value };

    public static StockResources operator -(StockResources a)
        => new() { Quantity = -a.Quantity, Value = -a.Value };

    public static StockResources operator -(StockResources a, StockResources b) => a + (-b);

    public void EnsureNonNegative()
    {
        if (Quantity < 0)
            throw new InsufficientBalanceException($"{nameof(Quantity)} cannot be negative ({Quantity}).");
        if (Value < 0)
            throw new InsufficientBalanceException($"{nameof(Value)} cannot be negative ({Value}).");
    }

    public void EnsureValidBalance()
    {
        if (Quantity < 0)
            throw new InsufficientBalanceException($"Stock {nameof(Quantity)} balance cannot be negative ({Quantity}).");
        if (Value < 0)
            throw new InsufficientBalanceException($"Stock {nameof(Value)} balance cannot be negative ({Value}).");
    }

    public bool IsZero => Quantity == 0m && Value == 0m;
}

[KandraRegisterEntity(TableName = "Reg_Stock_Balance")]
public sealed class StockBalance : Balance<StockDimensions, StockResources>
{
}

[KandraRegisterEntity(TableName = "Reg_Stock")]
[RegisterCaption("Stock")]
public sealed class StockMovement : Movement<StockDimensions, StockResources, NoDetails>
{
}

[Description("Accumulation balance register tracking quantity and value of pantry products per location and best-before date.")]
public sealed partial class StockRegister : BalanceRegister<StockMovement, StockBalance>
{
}
