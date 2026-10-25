using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Accidents")]
[KandraGeneratedEquality]
public sealed partial class Accident : DocumentBase, IEntityWithTypeId, IEntityWithRows<Accident>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Accident);

    public Accident() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Accident, object>>> RowNavigations => [];
    public void RemoveOrphans(Accident fetched, IRowRemover remover) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public int Odometer { get; set; }

    public string Place { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Precision(14, 2)]
    public decimal EstimatedDamage { get; set; }

    public string? Photo { get; set; }
}
