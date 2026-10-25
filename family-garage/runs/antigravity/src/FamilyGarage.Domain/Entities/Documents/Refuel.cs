using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Refuels")]
[KandraGeneratedEquality]
public sealed partial class Refuel : DocumentBase, IEntityWithTypeId, IEntityWithRows<Refuel>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Refuel);

    public Refuel() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Refuel, object>>> RowNavigations => [];
    public void RemoveOrphans(Refuel fetched, IRowRemover remover) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public Guid FuelStationId { get; set; }
    [JsonIgnore] public Provider? FuelStation { get; set; }

    [Precision(14, 2)]
    public decimal Litres { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public int Odometer { get; set; }

    public bool FullTank { get; set; }
}
