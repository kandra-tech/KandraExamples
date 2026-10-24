using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Currencies")]
[KandraGeneratedEquality]
public sealed partial class Currency : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(PersonalAccountingTypeIds.Currency);
}
