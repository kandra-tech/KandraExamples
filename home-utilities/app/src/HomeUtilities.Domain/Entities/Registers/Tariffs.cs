using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class TariffDimensions : IRegisterDimensions<TariffDimensions>
{
    [Caption("Tariff_Service")]
    [Description("The utility service for which this tariff applies.")]
    public Guid ServiceId { get; set; }
    [JsonIgnore]
    public Service Service { get; set; } = null!;

    [Caption("Tariff_Zone")]
    [Description("The tariff zone: Single, Day, or Night.")]
    public TariffZone Zone { get; set; }
}

[KandraGeneratedEquality]
public sealed partial class TariffDetails : IRegisterDetails<TariffDetails>
{
    [Precision(18, 4)]
    [Caption("Tariff_Price")]
    [Format("N4")]
    [Description("Tariff price per unit in UAH, valid from Period until next rate change.")]
    public decimal Price { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_Tariffs")]
[RegisterCaption("Tariffs")]
public sealed class TariffRecord : DocumentInfoRecord<TariffDimensions, TariffDetails>
{
}

[Description("Periodic information register of utility prices per service and zone valid from document date.")]
public sealed partial class TariffsRegister : DocumentInfoRegister<TariffRecord>
{
}
