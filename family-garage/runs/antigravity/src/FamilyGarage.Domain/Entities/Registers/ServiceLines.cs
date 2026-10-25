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
public sealed partial class ServiceLinesDimensions : IRegisterDimensions<ServiceLinesDimensions>
{
    [Caption("Vehicle")]
    [Description("The vehicle that received the service line.")]
    public Guid VehicleId { get; set; }

    [JsonIgnore]
    public Vehicle Vehicle { get; set; } = null!;

    [Caption("Item")]
    [Description("The part, work, or consumable item replaced or serviced.")]
    public Guid ItemId { get; set; }

    [JsonIgnore]
    public ItemNode Item { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class ServiceLinesResources : IRegisterResources<ServiceLinesResources>
{
    [Precision(18, 3)]
    [Caption("Quantity")]
    [Format("N3")]
    [Description("Quantity of the item serviced or replaced.")]
    public decimal Quantity { get; init; }

    [Precision(18, 2)]
    [Caption("Amount")]
    [Format("N2")]
    [Description("Cost amount of the service line in base currency.")]
    public decimal Amount { get; init; }

    public static ServiceLinesResources AdditiveIdentity => new();

    public static ServiceLinesResources operator +(ServiceLinesResources a, ServiceLinesResources b)
        => new() { Quantity = a.Quantity + b.Quantity, Amount = a.Amount + b.Amount };

    public static ServiceLinesResources operator -(ServiceLinesResources a)
        => new() { Quantity = -a.Quantity, Amount = -a.Amount };

    public static ServiceLinesResources operator -(ServiceLinesResources a, ServiceLinesResources b)
        => a + (-b);

    public void EnsureNonNegative()
    {
        if (Quantity < 0)
            throw new InsufficientBalanceException($"{nameof(Quantity)} cannot be negative ({Quantity}).");
        if (Amount < 0)
            throw new InsufficientBalanceException($"{nameof(Amount)} cannot be negative ({Amount}).");
    }

    public bool IsZero => Quantity == 0m && Amount == 0m;
}

[KandraGeneratedEquality]
public sealed partial class ServiceLinesDetails : IRegisterDetails<ServiceLinesDetails>
{
    [Caption("Odometer")]
    [Format("N0")]
    [Description("Vehicle odometer reading at the time of service, in kilometres.")]
    public int Odometer { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_ServiceLines")]
[RegisterCaption("ServiceLines")]
public sealed class ServiceLinesMovement : Movement<ServiceLinesDimensions, ServiceLinesResources, ServiceLinesDetails>
{
}

[Description("Turnover register recording detailed service lines: items replaced or serviced per vehicle and date.")]
public sealed partial class ServiceLinesRegister : TurnoverRegister<ServiceLinesMovement>
{
}
