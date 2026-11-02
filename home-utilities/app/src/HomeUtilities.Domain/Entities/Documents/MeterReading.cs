using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_MeterReadings")]
[KandraGeneratedEquality]
public sealed partial class MeterReading : DocumentBase, IEntityWithRows<MeterReading>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.MeterReading);

    public MeterReading() : base(TypeId) { }

    public Guid PropertyId { get; set; }

    [JsonIgnore]
    public Property Property { get; set; } = null!;

    public string? Description { get; set; }

    [KandraTablePart(TableName = "Doc_MeterReadingLines")]
    public IList<MeterReadingLine> Lines { get; set; } = new List<MeterReadingLine>();
}

[KandraGeneratedEquality]
public partial class MeterReadingLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid MeterReadingId { get; set; }

    [JsonIgnore]
    public MeterReading MeterReading { get; set; } = null!;

    public Guid MeterId { get; set; }

    [JsonIgnore]
    public Meter Meter { get; set; } = null!;

    public TariffZone Zone { get; set; }

    [Precision(18, 3)]
    public decimal Reading { get; set; }

    [Precision(18, 3)]
    public decimal PreviousReading { get; set; }
}
