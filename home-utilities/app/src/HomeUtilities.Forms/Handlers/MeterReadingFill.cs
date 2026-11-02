using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Documents;

namespace HomeUtilities.Forms.Handlers;

public sealed class MeterReadingFillRequestDto
{
    public Guid PropertyId { get; set; }
    public DateTime Date { get; set; }
}

public sealed class MeterReadingFillLineDto
{
    public Guid MeterId { get; set; }
    public TariffZone Zone { get; set; }
    public decimal PreviousReading { get; set; }
}

public sealed class MeterReadingFillResponseDto
{
    public List<MeterReadingFillLineDto> Lines { get; set; } = [];
}

[KandraHandler(Name = "MeterReadingFill")]
[PermissionsFrom(typeof(MeterReadingDto), ApplicationAction.View)]
public sealed class MeterReadingFillHandler : IHandler<MeterReadingFillRequestDto, MeterReadingFillResponseDto>;
