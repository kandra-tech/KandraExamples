using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_FinancePayments")]
[KandraGeneratedEquality]
public sealed partial class FinancePayment : DocumentBase, IEntityWithTypeId, IEntityWithRows<FinancePayment>
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.FinancePayment);

    public FinancePayment() : base(TypeId) { }

    public static IEnumerable<Expression<Func<FinancePayment, object>>> RowNavigations => [];
    public void RemoveOrphans(FinancePayment fetched, IRowRemover remover) { }

    public Guid FinanceContractId { get; set; }
    [JsonIgnore] public FinanceContract? FinanceContract { get; set; }

    [Precision(14, 2)]
    public decimal Principal { get; set; }

    [Precision(14, 2)]
    public decimal Interest { get; set; }

    [Precision(14, 2)]
    public decimal Fees { get; set; }

    public string? Comment { get; set; }
}
