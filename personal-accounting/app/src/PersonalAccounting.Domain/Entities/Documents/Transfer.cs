using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Transfer")]
[KandraGeneratedEquality]
public sealed partial class Transfer : DocumentBase, IEntityWithTypeId, IEntityWithRows<Transfer>
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Transfer);

    public Transfer() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Transfer, object>>> RowNavigations => [];

    public void RemoveOrphans(Transfer fetched, IRowRemover remover) { }

    public Guid FromAccountId { get; set; }
    [JsonIgnore]
    public Account FromAccount { get; set; } = null!;

    public Guid ToAccountId { get; set; }
    [JsonIgnore]
    public Account ToAccount { get; set; } = null!;

    [Precision(18, 2)]
    public decimal AmountOut { get; set; }

    [Precision(18, 2)]
    public decimal AmountIn { get; set; }

    public string? Comment { get; set; }
}
