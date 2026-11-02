using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Policies")]
[KandraGeneratedEquality]
public sealed partial class Policy : DocumentBase, IEntityWithTypeId, IEntityWithRows<Policy>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Policy);

    public Policy() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Policy, object>>> RowNavigations => [];
    public void RemoveOrphans(Policy fetched, IRowRemover remover) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public PolicyKind Kind { get; set; } = PolicyKind.CompulsoryInsurance;

    public Guid ProviderId { get; set; }
    [JsonIgnore] public Provider? Provider { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public string? ScannedDocument { get; set; }
}
