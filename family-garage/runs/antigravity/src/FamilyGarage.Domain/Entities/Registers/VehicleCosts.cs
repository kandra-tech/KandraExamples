using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Exceptions;
using Kandra.Domain.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class VehicleCostsDimensions : IRegisterDimensions<VehicleCostsDimensions>
{
    [Caption("Vehicle")]
    [Description("The vehicle for which the cost was incurred.")]
    public Guid VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle Vehicle { get; set; } = null!;

    [Caption("Category")]
    [Description("The category of the vehicle cost.")]
    public CostCategory Category { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class VehicleCostsResources : IRegisterResources<VehicleCostsResources>
{
    [Precision(18, 2)]
    [Caption("Amount")]
    [Format("N2")]
    [Description("Cost amount in base currency; never negative on a movement, IsExpense=true indicates a reduction (e.g. sale price).")]
    public decimal Amount { get; init; }

    public static VehicleCostsResources AdditiveIdentity => new();

    public static VehicleCostsResources operator +(VehicleCostsResources a, VehicleCostsResources b)
        => new() { Amount = a.Amount + b.Amount };

    public static VehicleCostsResources operator -(VehicleCostsResources a)
        => new() { Amount = -a.Amount };

    public static VehicleCostsResources operator -(VehicleCostsResources a, VehicleCostsResources b)
        => a + (-b);

    public void EnsureNonNegative()
    {
        if (Amount < 0)
            throw new InsufficientBalanceException($"{nameof(Amount)} cannot be negative ({Amount}).");
    }

    public bool IsZero => Amount == 0m;
}

[KandraGeneratedEquality]
public sealed partial class VehicleCostsDetails : IRegisterDetails<VehicleCostsDetails>
{
    [Precision(18, 2)]
    [Caption("Litres")]
    [Format("N2")]
    [Description("Fuel quantity in litres if applicable (e.g. for refuels).")]
    public decimal? Litres { get; set; }

    [Caption("FullTank")]
    [Description("Whether refuel was filled to full tank.")]
    public bool? FullTank { get; set; }

    [Caption("Odometer")]
    [Format("N0")]
    [Description("Odometer reading at the time of the event.")]
    public int? Odometer { get; set; }

    [Caption("IsSale")]
    [Description("True if this movement represents a vehicle sale price reducing ownership costs.")]
    public bool IsSale { get; set; }

    [Caption("Comment")]
    [Description("Optional notes or description.")]
    public string? Comment { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_VehicleCosts")]
[RegisterCaption("VehicleCosts")]
public sealed class VehicleCostsMovement : Movement<VehicleCostsDimensions, VehicleCostsResources, VehicleCostsDetails>
{
}

[Description("Accumulation turnover register tracking costs per vehicle and cost category over time.")]
public sealed partial class VehicleCostsRegister : TurnoverRegister<VehicleCostsMovement>
{
}
