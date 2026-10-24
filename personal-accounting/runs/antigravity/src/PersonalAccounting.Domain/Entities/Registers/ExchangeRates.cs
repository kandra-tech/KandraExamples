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
public sealed partial class ExchangeRatesDimensions : IRegisterDimensions<ExchangeRatesDimensions>
{
    [Caption("Dimension_Currency")]
    [Description("The currency for which the exchange rate to the base currency is set.")]
    public Guid CurrencyId { get; set; }

    [JsonIgnore]
    public Currency Currency { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class ExchangeRatesDetails : IRegisterDetails<ExchangeRatesDetails>
{
    [Precision(18, 6)]
    [Caption("Detail_Rate")]
    [Format("N4")]
    [Description("Exchange rate to the base currency valid from Period until the next rate record.")]
    public decimal Rate { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_ExchangeRates")]
[RegisterCaption("Reg_ExchangeRates")]
public sealed class ExchangeRatesRecord : DocumentInfoRecord<ExchangeRatesDimensions, ExchangeRatesDetails>
{
}

[Description("Document-bound periodic information register storing exchange rates to base currency valid from a date.")]
public sealed partial class ExchangeRatesRegister : DocumentInfoRegister<ExchangeRatesRecord>
{
}
