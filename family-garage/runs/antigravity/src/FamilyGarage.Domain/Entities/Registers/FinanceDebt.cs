using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class FinanceDebtDimensions : IRegisterDimensions<FinanceDebtDimensions>
{
    [Caption("FinanceContract")]
    [Description("The finance contract whose debt balance is tracked.")]
    public Guid ContractId { get; set; }

    [JsonIgnore]
    public FinanceContract Contract { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class FinanceDebtResources : IRegisterResources<FinanceDebtResources>
{
    [Precision(18, 2)]
    [Caption("Amount")]
    [Format("N2")]
    [Description("Remaining principal debt owed on the finance contract in base currency.")]
    public decimal Amount { get; init; }

    public static FinanceDebtResources AdditiveIdentity => new();

    public static FinanceDebtResources operator +(FinanceDebtResources a, FinanceDebtResources b)
        => new() { Amount = a.Amount + b.Amount };

    public static FinanceDebtResources operator -(FinanceDebtResources a)
        => new() { Amount = -a.Amount };

    public static FinanceDebtResources operator -(FinanceDebtResources a, FinanceDebtResources b)
        => a + (-b);

    public void EnsureNonNegative()
    {
        if (Amount < 0)
            throw new InsufficientBalanceException($"{nameof(Amount)} cannot be negative ({Amount}).");
    }

    public void EnsureValidBalance()
    {
        if (Amount < 0)
            throw new InsufficientBalanceException($"Debt balance on contract cannot go below zero ({Amount}).");
    }

    public bool IsZero => Amount == 0m;
}

[KandraRegisterEntity(TableName = "Reg_FinanceDebt_Balance")]
public sealed class FinanceDebtBalance : Balance<FinanceDebtDimensions, FinanceDebtResources>
{
}

[KandraRegisterEntity(TableName = "Reg_FinanceDebt")]
[RegisterCaption("FinanceDebt")]
public sealed class FinanceDebtMovement : Movement<FinanceDebtDimensions, FinanceDebtResources, NoDetails>
{
}

[Description("Accumulation balance register tracking remaining debt owed per finance contract.")]
public sealed partial class FinanceDebtRegister : BalanceRegister<FinanceDebtMovement, FinanceDebtBalance>
{
}
