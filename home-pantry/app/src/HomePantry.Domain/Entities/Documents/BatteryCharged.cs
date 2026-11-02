using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_BatteriesCharged")]
[KandraGeneratedEquality]
public sealed partial class BatteryCharged : DocumentBase, IEntityWithRows<BatteryCharged>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.BatteryCharged);

    public BatteryCharged() : base(TypeId) { }

    public Guid BatteryId { get; set; }

    [JsonIgnore]
    public Battery Battery { get; set; } = null!;

    public bool Replaced { get; set; }

    public static IEnumerable<Expression<Func<BatteryCharged, object>>> RowNavigations => [];
    public void RemoveOrphans(BatteryCharged fetched, IRowRemover remover) { }
}
