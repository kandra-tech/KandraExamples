using System.ComponentModel;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using HomePantry.Domain.Entities.Dictionaries;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Domain.Entities.Registers;

[KandraGeneratedEquality]
public sealed partial class LastPriceDimensions : IRegisterDimensions<LastPriceDimensions>
{
    [Caption("Product")]
    [Description("The product whose price is recorded.")]
    public Guid ProductId { get; set; }

    [JsonIgnore]
    public ProductNode Product { get; set; } = null!;

    [Caption("Store")]
    [Description("The store where the product was purchased at this price.")]
    public Guid StoreId { get; set; }

    [JsonIgnore]
    public Store Store { get; set; } = null!;
}

[KandraGeneratedEquality]
public sealed partial class LastPriceDetails : IRegisterDetails<LastPriceDetails>
{
    [Precision(14, 2)]
    [Caption("Price")]
    [Format("N2")]
    [Description("Purchase price per unit in home currency (UAH). Valid from Period until the next record.")]
    public decimal Price { get; set; }
}

[KandraRegisterEntity(TableName = "Reg_LastPrice")]
[RegisterCaption("LastPrice")]
public sealed class LastPriceRecord : DocumentInfoRecord<LastPriceDimensions, LastPriceDetails>
{
}

[Description("Periodic information register storing the last purchase price per product and store valid from the document date.")]
public sealed partial class LastPriceRegister : DocumentInfoRegister<LastPriceRecord>
{
}
