using Kandra.Application.Abstractions.Navigation;
using PersonalAccounting.Forms.Dashboards;
using PersonalAccounting.Forms.DataProcessors;
using PersonalAccounting.Forms.Dictionaries;
using PersonalAccounting.Forms.Documents;
using PersonalAccounting.Forms.Reports;

namespace PersonalAccounting.Application.Navigation;

public static class PersonalAccountingInterfaces
{
    public static InterfaceDefinition Default { get; } = new(
        Name: "Default",
        Title: new InterfaceNodeText("Default", IsLiteral: true),
        Visibility: null,
        HomeDashboard: typeof(PersonalAccountingDashboardDto),
        Nodes:
        [
            InterfaceNodeDefinition.Folder("Nav_QuickAdd", "AddCircleOutline",
                InterfaceNodeDefinition.Link("/document/Income/create", new InterfaceNodeText("Nav_AddIncome"), "Add"),
                InterfaceNodeDefinition.Link("/document/Expense/create", new InterfaceNodeText("Nav_AddExpense"), "Remove"),
                InterfaceNodeDefinition.Link("/document/Transfer/create", new InterfaceNodeText("Nav_AddTransfer"), "SwapHoriz")),

            InterfaceNodeDefinition.Folder("Nav_Documents", "Assignment",
                InterfaceNodeDefinition.Entity(typeof(IncomeDto), icon: "TrendingUp"),
                InterfaceNodeDefinition.Entity(typeof(ExpenseDto), icon: "TrendingDown"),
                InterfaceNodeDefinition.Entity(typeof(TransferDto), icon: "CompareArrows"),
                InterfaceNodeDefinition.Entity(typeof(BudgetDto), icon: "AccountBalanceWallet"),
                InterfaceNodeDefinition.Entity(typeof(OpeningBalancesDto), icon: "PlaylistAddCheck"),
                InterfaceNodeDefinition.Entity(typeof(SetExchangeRatesDto), icon: "CurrencyExchange")),

            InterfaceNodeDefinition.Folder("Nav_Catalogs", "Folder",
                InterfaceNodeDefinition.Entity(typeof(AccountDto), icon: "AccountBalance"),
                InterfaceNodeDefinition.Entity(typeof(CategoryNodeDto), icon: "Category"),
                InterfaceNodeDefinition.Entity(typeof(CurrencyDto), icon: "Paid")),

            InterfaceNodeDefinition.Folder("Nav_Reports", "Assessment",
                InterfaceNodeDefinition.Entity(typeof(ReportTransactionJournalDto), icon: "ReceiptLong"),
                InterfaceNodeDefinition.Entity(typeof(ReportBudgetVsActualDto), icon: "BarChart"),
                InterfaceNodeDefinition.Entity(typeof(ReportSpendingByCategoryDto), icon: "PieChart"),
                InterfaceNodeDefinition.Entity(typeof(ReportAccountStatementDto), icon: "RequestQuote"),
                InterfaceNodeDefinition.Entity(typeof(ReportExchangeRatesDto), icon: "ShowChart")),

            InterfaceNodeDefinition.Folder("Nav_Settings", "Settings",
                InterfaceNodeDefinition.Entity(typeof(CompanySettingsDto), icon: "Business"))
        ]);
}
