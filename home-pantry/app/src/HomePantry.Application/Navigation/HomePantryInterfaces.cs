using Kandra.Application.Abstractions.Navigation;
using HomePantry.Forms.Dashboards;
using HomePantry.Forms.DataProcessors;
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Documents;
using HomePantry.Forms.Reports;

namespace HomePantry.Application.Navigation;

public static class HomePantryInterfaces
{
    public static InterfaceDefinition Default { get; } = new(
        Name: "Default",
        Title: new InterfaceNodeText("Nav_DefaultInterface"),
        Visibility: null,
        HomeDashboard: typeof(PantryDashboardDto),
        Nodes:
        [
            InterfaceNodeDefinition.Folder("Nav_QuickAdd", "FlashOn",
                InterfaceNodeDefinition.Link("/document/purchase/create", new InterfaceNodeText("Nav_QuickAddPurchase"), icon: "AddShoppingCart"),
                InterfaceNodeDefinition.Link("/document/consume/create", new InterfaceNodeText("Nav_QuickAddConsume"), icon: "RemoveShoppingCart")),

            InterfaceNodeDefinition.Folder("Nav_Documents", "ReceiptLong",
                InterfaceNodeDefinition.Entity(typeof(PurchaseDto), icon: "ShoppingCart"),
                InterfaceNodeDefinition.Entity(typeof(ConsumeDto), icon: "Restaurant"),
                InterfaceNodeDefinition.Entity(typeof(TransferDto), icon: "SwapHoriz"),
                InterfaceNodeDefinition.Entity(typeof(InventoryCountDto), icon: "FactCheck"),
                InterfaceNodeDefinition.Entity(typeof(CookRecipeDto), icon: "SoupKitchen"),
                InterfaceNodeDefinition.Entity(typeof(ShoppingListDto), icon: "Checklist")),

            InterfaceNodeDefinition.Folder("Nav_Catalogs", "Folder",
                InterfaceNodeDefinition.Entity(typeof(ProductNodeDto), icon: "Category"),
                InterfaceNodeDefinition.Entity(typeof(LocationNodeDto), icon: "Place"),
                InterfaceNodeDefinition.Entity(typeof(StoreDto), icon: "Store"),
                InterfaceNodeDefinition.Entity(typeof(RecipeDto), icon: "MenuBook")),

            InterfaceNodeDefinition.Folder("Nav_Reports", "Assessment",
                InterfaceNodeDefinition.Entity(typeof(ReportStockOverviewDto), icon: "Inventory"),
                InterfaceNodeDefinition.Entity(typeof(ReportExpiringDto), icon: "HourglassBottom"),
                InterfaceNodeDefinition.Entity(typeof(ReportShoppingNeedsDto), icon: "AddShoppingCart"),
                InterfaceNodeDefinition.Entity(typeof(ReportCanICookItDto), icon: "CheckCircle"),
                InterfaceNodeDefinition.Entity(typeof(ReportSpendingDto), icon: "AttachMoney")),

            InterfaceNodeDefinition.Folder("Nav_Settings", "Settings",
                InterfaceNodeDefinition.Entity(typeof(CompanySettingsDto), icon: "Business"))
        ]);
}
