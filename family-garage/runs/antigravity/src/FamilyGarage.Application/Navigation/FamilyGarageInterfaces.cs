using Kandra.Application.Abstractions.Navigation;
using FamilyGarage.Forms.Dashboards;
using FamilyGarage.Forms.DataProcessors;
using FamilyGarage.Forms.Dictionaries;
using FamilyGarage.Forms.Documents;
using FamilyGarage.Forms.Reports;

namespace FamilyGarage.Application.Navigation;

public static class FamilyGarageInterfaces
{
    public static InterfaceDefinition Default { get; } = new(
        Name: "Default",
        Title: new InterfaceNodeText("Default", IsLiteral: true),
        Visibility: null,
        HomeDashboard: typeof(GarageDashboardDto),
        Nodes:
        [
            InterfaceNodeDefinition.Folder("Nav_QuickAdd", "FlashOn",
                InterfaceNodeDefinition.Link("/document/refuel/create", new InterfaceNodeText("Nav_QuickAdd_Refuel"), "LocalGasStation"),
                InterfaceNodeDefinition.Link("/document/service/create", new InterfaceNodeText("Nav_QuickAdd_Service"), "Build"),
                InterfaceNodeDefinition.Link("/document/financepayment/create", new InterfaceNodeText("Nav_QuickAdd_FinancePayment"), "Payment")),

            InterfaceNodeDefinition.Folder("Nav_Documents", "Description",
                InterfaceNodeDefinition.Entity(typeof(VehiclePurchaseDto)),
                InterfaceNodeDefinition.Entity(typeof(FinancePaymentDto)),
                InterfaceNodeDefinition.Entity(typeof(VehicleSaleDto)),
                InterfaceNodeDefinition.Entity(typeof(RefuelDto)),
                InterfaceNodeDefinition.Entity(typeof(ServiceDto)),
                InterfaceNodeDefinition.Entity(typeof(AccidentDto)),
                InterfaceNodeDefinition.Entity(typeof(PolicyDto))),

            InterfaceNodeDefinition.Folder("Nav_Reports", "Assessment",
                InterfaceNodeDefinition.Entity(typeof(ReportUpcomingDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportTotalCostOfOwnershipDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportCostsPivotDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportFinancingDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportVehicleHistoryDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportFuelConsumptionDto))),

            InterfaceNodeDefinition.Folder("Nav_Dictionaries", "Folder",
                InterfaceNodeDefinition.Entity(typeof(VehicleDto)),
                InterfaceNodeDefinition.Entity(typeof(ItemNodeDto)),
                InterfaceNodeDefinition.Entity(typeof(ProviderDto)),
                InterfaceNodeDefinition.Entity(typeof(FinanceContractDto))),

            InterfaceNodeDefinition.Folder("Nav_DataProcessors", "Build",
                InterfaceNodeDefinition.Entity(typeof(UpcomingRemindersDto)),
                InterfaceNodeDefinition.Entity(typeof(CompanySettingsDto))),
        ]);
}
