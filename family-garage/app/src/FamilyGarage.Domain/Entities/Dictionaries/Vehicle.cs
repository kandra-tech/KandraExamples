using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;

namespace FamilyGarage.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Vehicles")]
[KandraGeneratedEquality]
public partial class Vehicle : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Vehicle);

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Vin { get; set; } = string.Empty;
    public FuelType FuelType { get; set; }
    public string? Photo { get; set; }
}
