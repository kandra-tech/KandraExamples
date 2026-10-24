using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_SetExchangeRates")]
[KandraGeneratedEquality]
public sealed partial class SetExchangeRates : DocumentBase, IEntityWithRows<SetExchangeRates>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.SetExchangeRates);

    public SetExchangeRates() : base(TypeId) { }

    public string? Description { get; set; }

    [KandraTablePart(TableName = "Doc_SetExchangeRatesLines")]
    public IList<SetExchangeRatesLine> Lines { get; set; } = new List<SetExchangeRatesLine>();
}

[KandraGeneratedEquality]
public partial class SetExchangeRatesLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid SetExchangeRatesId { get; set; }
    [JsonIgnore]
    public SetExchangeRates SetExchangeRates { get; set; } = null!;

    public Guid CurrencyId { get; set; }
    [JsonIgnore]
    public Currency Currency { get; set; } = null!;

    [Precision(18, 6)]
    public decimal Rate { get; set; }
}
