using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;

namespace FamilyGarage.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Providers")]
[KandraGeneratedEquality]
public partial class Provider : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Provider);

    public ProviderKind Kind { get; set; }
    public string? Phone { get; set; }
    public string? Comment { get; set; }
}
