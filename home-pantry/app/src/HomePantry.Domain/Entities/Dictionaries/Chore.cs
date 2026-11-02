using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Chores")]
[KandraGeneratedEquality]
public sealed partial class Chore : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.Chore);

    public ChoreRecurrence Recurrence { get; set; } = ChoreRecurrence.Daily;

    public int EveryNDays { get; set; } = 1;

    public Guid? AssignedToId { get; set; }

    [JsonIgnore]
    public HouseholdMember? AssignedTo { get; set; }

    public string? Description { get; set; }
}
