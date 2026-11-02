using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Budget")]
[KandraGeneratedEquality]
public sealed partial class Budget : DocumentBase, IEntityWithRows<Budget>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Budget);

    public Budget() : base(TypeId) { }

    public string? Description { get; set; }

    [KandraTablePart(TableName = "Doc_BudgetLines")]
    public IList<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}

[KandraGeneratedEquality]
public partial class BudgetLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid BudgetId { get; set; }
    [JsonIgnore]
    public Budget Budget { get; set; } = null!;

    public Guid CategoryId { get; set; }
    [JsonIgnore]
    public CategoryNode Category { get; set; } = null!;

    [Precision(18, 2)]
    public decimal PlannedAmount { get; set; }
}
