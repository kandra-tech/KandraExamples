using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_VehicleSales")]
[KandraGeneratedEquality]
public sealed partial class VehicleSale : DocumentBase, IEntityWithTypeId, IEntityWithRows<VehicleSale>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.VehicleSale);

    public VehicleSale() : base(TypeId) { }

    public static IEnumerable<Expression<Func<VehicleSale, object>>> RowNavigations => [];
    public void RemoveOrphans(VehicleSale fetched, IRowRemover remover) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public string Buyer { get; set; } = string.Empty;

    [Precision(14, 2)]
    public decimal Price { get; set; }

    public int Odometer { get; set; }
}
