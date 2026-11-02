using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_SetTariffs")]
[KandraGeneratedEquality]
public sealed partial class SetTariffs : DocumentBase, IEntityWithRows<SetTariffs>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.SetTariffs);

    public SetTariffs() : base(TypeId) { }

    public string? Description { get; set; }

    [KandraTablePart(TableName = "Doc_SetTariffsLines")]
    public IList<SetTariffsLine> Lines { get; set; } = new List<SetTariffsLine>();
}

[KandraGeneratedEquality]
public partial class SetTariffsLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid SetTariffsId { get; set; }

    [JsonIgnore]
    public SetTariffs SetTariffs { get; set; } = null!;

    public Guid ServiceId { get; set; }

    [JsonIgnore]
    public Service Service { get; set; } = null!;

    public TariffZone Zone { get; set; }

    [Precision(18, 4)]
    public decimal Price { get; set; }
}
