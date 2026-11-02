using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class UtilityCostDimensions : IRegisterDimensions<UtilityCostDimensions>
{
    [Caption("UtilityCost_Property")]
    [Description("The property where the utility was consumed.")]
    public Guid PropertyId { get; set; }
    [JsonIgnore]
    public Property Property { get; set; } = null!;

    [Caption("UtilityCost_Service")]
    [Description("The utility service consumed.")]
    public Guid ServiceId { get; set; }
    [JsonIgnore]
    public Service Service { get; set; } = null!;

    [Caption("UtilityCost_Zone")]
    [Description("The tariff zone (Single, Day, Night).")]
    public TariffZone Zone { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class UtilityCostResources : IRegisterResources<UtilityCostResources>
{
    [Precision(18, 3)]
    [Caption("UtilityCost_Quantity")]
    [Format("N3")]
    [Description("Consumption quantity in service units.")]
    public decimal Quantity { get; init; }

    [Precision(18, 2)]
    [Caption("UtilityCost_Amount")]
    [Format("N2")]
    [Description("Cost amount in UAH.")]
    public decimal Amount { get; init; }

    public static UtilityCostResources AdditiveIdentity => new();

    public static UtilityCostResources operator +(UtilityCostResources a, UtilityCostResources b)
        => new() { Quantity = a.Quantity + b.Quantity, Amount = a.Amount + b.Amount };

    public static UtilityCostResources operator -(UtilityCostResources a)
        => new() { Quantity = -a.Quantity, Amount = -a.Amount };

    public static UtilityCostResources operator -(UtilityCostResources a, UtilityCostResources b) => a + (-b);

    public void EnsureNonNegative()
    {
        if (Quantity < 0)
            throw new InsufficientBalanceException("Utility cost quantity cannot be negative.");
        if (Amount < 0)
            throw new InsufficientBalanceException("Utility cost amount cannot be negative.");
    }

    public void EnsureValidBalance()
    {
    }

    public bool IsZero => Quantity == 0m && Amount == 0m;
}

[KandraRegisterEntity(TableName = "Reg_UtilityCosts")]
[RegisterCaption("UtilityCosts")]
public sealed class UtilityCostMovement : Movement<UtilityCostDimensions, UtilityCostResources, NoDetails>
{
}

[Description("Accumulation register of turnovers tracking quantity and amount per property, service, and month.")]
public sealed partial class UtilityCostsRegister : TurnoverRegister<UtilityCostMovement>
{
}
