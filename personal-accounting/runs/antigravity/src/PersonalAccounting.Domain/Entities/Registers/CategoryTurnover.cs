using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;

namespace PersonalAccounting.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class CategoryTurnoverDimensions : IRegisterDimensions<CategoryTurnoverDimensions>
{
    [Caption("Dimension_Category")]
    [Description("The category of the turnover.")]
    public Guid CategoryId { get; set; }

    [JsonIgnore]
    public CategoryNode Category { get; set; } = null!;

    [Caption("Dimension_Account")]
    [Description("The account through which the turnover occurred.")]
    public Guid AccountId { get; set; }

    [JsonIgnore]
    public Account Account { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class CategoryTurnoverResources : IRegisterResources<CategoryTurnoverResources>
{
    [Precision(18, 2)]
    [Caption("Resource_AmountInAccountCurrency")]
    [Format("N2")]
    [Description("Turnover amount in the account's own currency.")]
    public decimal AmountInAccountCurrency { get; init; }

    [Precision(18, 2)]
    [Caption("Resource_AmountInBaseCurrency")]
    [Format("N2")]
    [Description("Turnover amount converted to base currency at document date rate.")]
    public decimal AmountInBaseCurrency { get; init; }

    public static CategoryTurnoverResources AdditiveIdentity => new();

    public static CategoryTurnoverResources operator +(CategoryTurnoverResources a, CategoryTurnoverResources b)
        => new()
        {
            AmountInAccountCurrency = a.AmountInAccountCurrency + b.AmountInAccountCurrency,
            AmountInBaseCurrency = a.AmountInBaseCurrency + b.AmountInBaseCurrency
        };

    public static CategoryTurnoverResources operator -(CategoryTurnoverResources a)
        => new()
        {
            AmountInAccountCurrency = -a.AmountInAccountCurrency,
            AmountInBaseCurrency = -a.AmountInBaseCurrency
        };

    public static CategoryTurnoverResources operator -(CategoryTurnoverResources a, CategoryTurnoverResources b) => a + (-b);

    public void EnsureNonNegative()
    {
        if (AmountInAccountCurrency < 0 || AmountInBaseCurrency < 0)
            throw new InsufficientBalanceException("Turnover amounts cannot be negative on a movement.");
    }

    public void EnsureValidBalance()
    {
    }

    public bool IsZero => AmountInAccountCurrency == 0m && AmountInBaseCurrency == 0m;
}

[KandraGeneratedEquality]
public sealed partial class CategoryTurnoverDetails : IRegisterDetails<CategoryTurnoverDetails>
{
    [Caption("Detail_Comment")]
    [Description("User comment or description.")]
    public string? Comment { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_CategoryTurnover")]
[RegisterCaption("Reg_CategoryTurnover")]
public sealed class CategoryTurnoverMovement : Movement<CategoryTurnoverDimensions, CategoryTurnoverResources, CategoryTurnoverDetails>
{
}

[Description("Turnover register recording amounts per category and account per day, in account currency and base currency.")]
public sealed partial class CategoryTurnoverRegister : TurnoverRegister<CategoryTurnoverMovement>
{
}
