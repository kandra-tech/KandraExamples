using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Entities.Dictionaries;

namespace PersonalAccounting.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class BudgetPlanDimensions : IRegisterDimensions<BudgetPlanDimensions>
{
    [Caption("Dimension_Category")]
    [Description("The expense category for which the budget amount is planned.")]
    public Guid CategoryId { get; set; }

    [JsonIgnore]
    public CategoryNode Category { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class BudgetPlanDetails : IRegisterDetails<BudgetPlanDetails>
{
    [Precision(18, 2)]
    [Caption("Detail_PlannedAmount")]
    [Format("N2")]
    [Description("Planned budget amount for the category in the budget's month.")]
    public decimal PlannedAmount { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_BudgetPlan")]
[RegisterCaption("Reg_BudgetPlan")]
public sealed class BudgetPlanRecord : DocumentInfoRecord<BudgetPlanDimensions, BudgetPlanDetails>
{
}

[Description("Document-bound periodic information register storing planned expense amounts per category per month.")]
public sealed partial class BudgetPlanRegister : DocumentInfoRegister<BudgetPlanRecord>
{
}
