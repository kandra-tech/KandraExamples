using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_FinanceContracts")]
[KandraGeneratedEquality]
public partial class FinanceContract : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.FinanceContract);

    public string Number { get; set; } = string.Empty;

    public Guid VehicleId { get; set; }
    [JsonIgnore]
    public virtual Vehicle? Vehicle { get; set; }

    public FinanceContractKind Kind { get; set; }

    public Guid LenderId { get; set; }
    [JsonIgnore]
    public virtual Provider? Lender { get; set; }

    public DateTime SignedOn { get; set; }

    [Precision(18, 4)]
    public decimal AnnualInterestRatePercent { get; set; }

    public int TermMonths { get; set; }

    [Precision(18, 2)]
    public decimal MonthlyPayment { get; set; }

    public int PaymentDayOfMonth { get; set; }

    public string? Comment { get; set; }
}
