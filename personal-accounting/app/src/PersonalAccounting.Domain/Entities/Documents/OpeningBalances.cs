using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_OpeningBalances")]
[KandraGeneratedEquality]
public sealed partial class OpeningBalances : DocumentBase, IEntityWithRows<OpeningBalances>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.OpeningBalances);

    public OpeningBalances() : base(TypeId) { }

    public string? Description { get; set; }

    [KandraTablePart(TableName = "Doc_OpeningBalancesLines")]
    public IList<OpeningBalancesLine> Lines { get; set; } = new List<OpeningBalancesLine>();
}

[KandraGeneratedEquality]
public partial class OpeningBalancesLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid OpeningBalancesId { get; set; }
    [JsonIgnore]
    public OpeningBalances OpeningBalances { get; set; } = null!;

    public Guid AccountId { get; set; }
    [JsonIgnore]
    public Account Account { get; set; } = null!;

    [Precision(18, 2)]
    public decimal Amount { get; set; }
}
