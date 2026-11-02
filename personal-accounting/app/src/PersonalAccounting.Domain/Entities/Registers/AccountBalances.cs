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
public sealed partial class AccountBalancesDimensions : IRegisterDimensions<AccountBalancesDimensions>
{
    [Caption("Dimension_Account")]
    [Description("The account whose balance is tracked.")]
    public Guid AccountId { get; set; }

    [JsonIgnore]
    public Account Account { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class AccountBalancesResources : IRegisterResources<AccountBalancesResources>
{
    [Precision(18, 2)]
    [Caption("Resource_Amount")]
    [Format("N2")]
    [Description("Account balance amount in the account's own currency.")]
    public decimal Amount { get; init; }

    public static AccountBalancesResources AdditiveIdentity => new();

    public static AccountBalancesResources operator +(AccountBalancesResources a, AccountBalancesResources b)
        => new() { Amount = a.Amount + b.Amount };

    public static AccountBalancesResources operator -(AccountBalancesResources a)
        => new() { Amount = -a.Amount };

    public static AccountBalancesResources operator -(AccountBalancesResources a, AccountBalancesResources b) => a + (-b);

    public void EnsureNonNegative()
    {
        if (Amount < 0)
            throw new InsufficientBalanceException("Amount cannot be negative on a movement.");
    }

    public void EnsureValidBalance()
    {
        // Credit cards and overdrafts may have negative balances
    }

    public bool IsZero => Amount == 0m;
}

[KandraGeneratedEquality]
public sealed partial class AccountBalancesDetails : IRegisterDetails<AccountBalancesDetails>
{
    [Caption("Detail_Category")]
    [Description("The income/expense category associated with the transaction.")]
    [ReferenceOf(typeof(CategoryNode))]
    public Guid? CategoryId { get; set; }

    [Caption("Detail_CounterAccount")]
    [Description("The counter-account in a transfer transaction.")]
    [ReferenceOf(typeof(Account))]
    public Guid? CounterAccountId { get; set; }

    [Caption("Detail_Comment")]
    [Description("User comment or description.")]
    public string? Comment { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_AccountBalances_Balance")]
public sealed class AccountBalancesBalance : Balance<AccountBalancesDimensions, AccountBalancesResources>
{
}

[KandraRegisterEntity(TableName = "Reg_AccountBalances")]
[RegisterCaption("Reg_AccountBalances")]
public sealed class AccountBalanceMovement : Movement<AccountBalancesDimensions, AccountBalancesResources, AccountBalancesDetails>
{
}

[Description("Accumulation register holding running balances per account in the account's own currency.")]
public sealed partial class AccountBalancesRegister : BalanceRegister<AccountBalanceMovement, AccountBalancesBalance>
{
}
