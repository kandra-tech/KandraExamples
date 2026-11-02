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
public sealed partial class BatteryCyclesDimensions : IRegisterDimensions<BatteryCyclesDimensions>
{
    [Caption("Battery")]
    [Description("The battery whose cycles and replacements are tracked.")]
    public Guid BatteryId { get; set; }

    [JsonIgnore]
    public Battery Battery { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class BatteryCyclesResources : IRegisterResources<BatteryCyclesResources>
{
    [Precision(14, 2)]
    [Caption("Cycles")]
    [Format("N0")]
    [Description("Charge cycles performed. Must not be negative.")]
    public decimal Cycles { get; init; }

    [Precision(14, 2)]
    [Caption("Replacements")]
    [Format("N0")]
    [Description("Battery replacements performed. Must not be negative.")]
    public decimal Replacements { get; init; }

    public static BatteryCyclesResources AdditiveIdentity => new();

    public static BatteryCyclesResources operator +(BatteryCyclesResources a, BatteryCyclesResources b)
        => new() { Cycles = a.Cycles + b.Cycles, Replacements = a.Replacements + b.Replacements };

    public static BatteryCyclesResources operator -(BatteryCyclesResources a)
        => new() { Cycles = -a.Cycles, Replacements = -a.Replacements };

    public static BatteryCyclesResources operator -(BatteryCyclesResources a, BatteryCyclesResources b)
        => a + (-b);

    public void EnsureNonNegative()
    {
        if (Cycles < 0)
            throw new InsufficientBalanceException($"{nameof(Cycles)} cannot be negative ({Cycles}).");
        if (Replacements < 0)
            throw new InsufficientBalanceException($"{nameof(Replacements)} cannot be negative ({Replacements}).");
    }

    public bool IsZero => Cycles == 0m && Replacements == 0m;
}

[KandraRegisterEntity(TableName = "Reg_BatteryCycles")]
[RegisterCaption("BatteryCycles")]
public sealed class BatteryCyclesMovement : Movement<BatteryCyclesDimensions, BatteryCyclesResources, NoDetails>
{
}

[Description("Accumulation turnover register tracking battery charge cycles and replacements per battery and day.")]
public sealed partial class BatteryCyclesRegister : TurnoverRegister<BatteryCyclesMovement>
{
}
