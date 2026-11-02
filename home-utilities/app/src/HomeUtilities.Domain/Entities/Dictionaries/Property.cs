using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Properties")]
[KandraGeneratedEquality]
public sealed partial class Property : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Property);

    public string? Address { get; set; }

    public decimal Area { get; set; }

    public int RegisteredPersonsCount { get; set; } = 1;

    public string? Owner { get; set; }
}
