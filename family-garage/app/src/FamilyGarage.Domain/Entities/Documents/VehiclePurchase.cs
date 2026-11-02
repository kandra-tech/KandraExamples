using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_VehiclePurchases")]
[KandraGeneratedEquality]
public sealed partial class VehiclePurchase : DocumentBase, IEntityWithTypeId, IEntityWithRows<VehiclePurchase>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.VehiclePurchase);

    public VehiclePurchase() : base(TypeId) { }

    public static IEnumerable<Expression<Func<VehiclePurchase, object>>> RowNavigations => [];
    public void RemoveOrphans(VehiclePurchase fetched, IRowRemover remover) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public Guid SellerId { get; set; }
    [JsonIgnore] public Provider? Seller { get; set; }

    [Precision(14, 2)]
    public decimal Price { get; set; }

    public int Odometer { get; set; }

    public VehiclePurchasePaymentKind Payment { get; set; } = VehiclePurchasePaymentKind.Cash;

    [Precision(14, 2)]
    public decimal DownPayment { get; set; }

    public Guid? FinanceContractId { get; set; }
    [JsonIgnore] public FinanceContract? FinanceContract { get; set; }
}
