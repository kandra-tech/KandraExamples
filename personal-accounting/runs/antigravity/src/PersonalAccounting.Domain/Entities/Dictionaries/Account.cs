using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Accounts")]
[KandraGeneratedEquality]
public sealed partial class Account : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Account);

    public AccountType Type { get; set; }

    public Guid CurrencyId { get; set; }

    [JsonIgnore]
    public Currency Currency { get; set; } = null!;
}
