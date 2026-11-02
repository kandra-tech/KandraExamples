using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;

namespace HomeUtilities.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class SettlementDimensions : IRegisterDimensions<SettlementDimensions>
{
    [Caption("Settlement_Provider")]
    [Description("The utility provider owed.")]
    public Guid ProviderId { get; set; }
    [JsonIgnore]
    public Provider Provider { get; set; } = null!;

    [Caption("Settlement_Property")]
    [Description("The property for which utility settlement is tracked.")]
    public Guid PropertyId { get; set; }
    [JsonIgnore]
    public Property Property { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class SettlementResources : IRegisterResources<SettlementResources>
{
    [Precision(18, 2)]
    [Caption("Settlement_Amount")]
    [Format("N2")]
    [Description("Outstanding debt to the provider in UAH. Positive balance is debt, negative balance is prepayment.")]
    public decimal Amount { get; init; }

    public static SettlementResources AdditiveIdentity => new();

    public static SettlementResources operator +(SettlementResources a, SettlementResources b)
        => new() { Amount = a.Amount + b.Amount };

    public static SettlementResources operator -(SettlementResources a)
        => new() { Amount = -a.Amount };

    public static SettlementResources operator -(SettlementResources a, SettlementResources b) => a + (-b);

    public void EnsureNonNegative()
    {
        if (Amount < 0)
            throw new InsufficientBalanceException("Settlement amount on a movement cannot be negative.");
    }

    // A negative balance is valid (prepayment to provider), so EnsureValidBalance is relaxed.
    public void EnsureValidBalance()
    {
    }

    public bool IsZero => Amount == 0m;
}

[KandraRegisterEntity(TableName = "Reg_Settlements_Balance")]
public sealed class SettlementBalance : Balance<SettlementDimensions, SettlementResources>
{
}

[KandraRegisterEntity(TableName = "Reg_Settlements")]
[RegisterCaption("Settlements")]
public sealed class SettlementMovement : Movement<SettlementDimensions, SettlementResources, NoDetails>
{
}

[Description("Accumulation register with balances tracking what the household owes per provider and property; negative balance represents prepayment.")]
public sealed partial class SettlementsRegister : BalanceRegister<SettlementMovement, SettlementBalance>
{
}
