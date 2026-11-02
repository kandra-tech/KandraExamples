using Kandra.Domain.Commons;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Enums;

namespace HomePantry.Persistence.Documents;

public sealed class HomePantryDocumentTypeResolver : IDocumentTypeResolver
{
    private static readonly Dictionary<Guid, Type> Types = new()
    {
        [new Guid(HomePantryTypeIds.Purchase)] = typeof(Purchase),
        [new Guid(HomePantryTypeIds.Consume)] = typeof(Consume),
        [new Guid(HomePantryTypeIds.Transfer)] = typeof(Transfer),
        [new Guid(HomePantryTypeIds.InventoryCount)] = typeof(InventoryCount),
        [new Guid(HomePantryTypeIds.CookRecipe)] = typeof(CookRecipe),
        [new Guid(HomePantryTypeIds.ShoppingList)] = typeof(ShoppingList),
        [new Guid(HomePantryTypeIds.ChoreDone)] = typeof(ChoreDone),
        [new Guid(HomePantryTypeIds.BatteryCharged)] = typeof(BatteryCharged),
        [new Guid(HomePantryTypeIds.MealPlan)] = typeof(MealPlan),
    };

    public Type? Resolve(Guid documentTypeId) => Types.GetValueOrDefault(documentTypeId);
}
