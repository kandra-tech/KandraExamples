using Kandra.Application.Abstractions.Navigation;
using HomeUtilities.Forms.Dashboards;
using HomeUtilities.Forms.DataProcessors;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Forms.Documents;
using HomeUtilities.Forms.Reports;

namespace HomeUtilities.Application.Navigation;

public static class HomeUtilitiesInterfaces
{
    public static InterfaceDefinition Default { get; } = new(
        Name: "Default",
        Title: new InterfaceNodeText("Default", IsLiteral: true),
        Visibility: null,
        HomeDashboard: typeof(HomeDashboardDto),
        Nodes:
        [
            InterfaceNodeDefinition.Folder("Nav_Documents", "Description",
                InterfaceNodeDefinition.Entity(typeof(MeterReadingDto)),
                InterfaceNodeDefinition.Entity(typeof(InvoiceDto)),
                InterfaceNodeDefinition.Entity(typeof(PaymentDto)),
                InterfaceNodeDefinition.Entity(typeof(SetTariffsDto)),
                InterfaceNodeDefinition.Link("/document/meterReading/create", new InterfaceNodeText("Nav_QuickAdd_MeterReading"), "Add"),
                InterfaceNodeDefinition.Link("/document/payment/create", new InterfaceNodeText("Nav_QuickAdd_Payment"), "Add")),

            InterfaceNodeDefinition.Folder("Nav_Reports", "Assessment",
                InterfaceNodeDefinition.Entity(typeof(ReportProviderStatementDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportConsumptionDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportCostsDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportTariffHistoryDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportMeterConsumptionDto)),
                InterfaceNodeDefinition.Entity(typeof(ReportMeterReadingsHistoryDto))),

            InterfaceNodeDefinition.Folder("Nav_ReferenceData", "Folder",
                InterfaceNodeDefinition.Entity(typeof(PropertyDto)),
                InterfaceNodeDefinition.Entity(typeof(ProviderDto)),
                InterfaceNodeDefinition.Entity(typeof(ServiceNodeDto)),
                InterfaceNodeDefinition.Entity(typeof(MeterDto))),

            InterfaceNodeDefinition.Folder("Nav_Administration", "Settings",
                InterfaceNodeDefinition.Entity(typeof(RemindersDto)),
                InterfaceNodeDefinition.Entity(typeof(CompanySettingsDto), icon: "Business")),
        ]);
}
