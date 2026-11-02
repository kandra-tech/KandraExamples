using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Expense")]
[KandraGeneratedEquality]
public sealed partial class Expense : DocumentBase, IEntityWithTypeId, IEntityWithRows<Expense>
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Expense);

    public Expense() : base(TypeId) { }

    public static IEnumerable<Expression<Func<Expense, object>>> RowNavigations => [];

    public void RemoveOrphans(Expense fetched, IRowRemover remover) { }

    public Guid AccountId { get; set; }
    [JsonIgnore]
    public Account Account { get; set; } = null!;

    public Guid CategoryId { get; set; }
    [JsonIgnore]
    public CategoryNode Category { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }
}
