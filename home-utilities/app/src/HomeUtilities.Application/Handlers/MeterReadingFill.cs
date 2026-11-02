using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Authorization;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Handlers;

namespace HomeUtilities.Application.Handlers;

[KandraHandlerBehavior(typeof(MeterReadingFillHandler))]
public sealed class MeterReadingFillBehavior(
    IEntityRepository<Meter> metersRepository,
    IInfoRegisterReader<MeterReadingRecord> meterReadingsReader,
    IAuthorizationChecker? authorizationChecker = null)
    : IHandlerBehavior<MeterReadingFillRequestDto, MeterReadingFillResponseDto>
{
    public async Task<MeterReadingFillResponseDto> HandleAsync(MeterReadingFillRequestDto request, CancellationToken cancellationToken)
    {
        authorizationChecker?.AuthorizeFrom(typeof(MeterReadingFillHandler));

        if (request.PropertyId == Guid.Empty)
            return new MeterReadingFillResponseDto();

        var meters = new List<Meter>();
        await foreach (var m in metersRepository.GetAllAsync(q => q.Where(m => m.PropertyId == request.PropertyId && !m.IsDeleted)))
        {
            meters.Add(m);
        }

        var result = new List<MeterReadingFillLineDto>();
        foreach (var meter in meters)
        {
            if (meter.Zones == MeterZones.Single)
            {
                var prev = await meterReadingsReader.SliceOfLast(request.Date.AddTicks(-1),
                    q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == TariffZone.Single),
                    cancellationToken);
                var prevVal = prev.Count > 0 ? prev[0].Details.Reading : 0m;
                result.Add(new MeterReadingFillLineDto
                {
                    MeterId = meter.Id,
                    Zone = TariffZone.Single,
                    PreviousReading = prevVal,
                });
            }
            else // DayNight
            {
                var prevDay = await meterReadingsReader.SliceOfLast(request.Date.AddTicks(-1),
                    q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == TariffZone.Day),
                    cancellationToken);
                var prevDayVal = prevDay.Count > 0 ? prevDay[0].Details.Reading : 0m;
                result.Add(new MeterReadingFillLineDto
                {
                    MeterId = meter.Id,
                    Zone = TariffZone.Day,
                    PreviousReading = prevDayVal,
                });

                var prevNight = await meterReadingsReader.SliceOfLast(request.Date.AddTicks(-1),
                    q => q.Where(r => r.Dimensions.MeterId == meter.Id && r.Dimensions.Zone == TariffZone.Night),
                    cancellationToken);
                var prevNightVal = prevNight.Count > 0 ? prevNight[0].Details.Reading : 0m;
                result.Add(new MeterReadingFillLineDto
                {
                    MeterId = meter.Id,
                    Zone = TariffZone.Night,
                    PreviousReading = prevNightVal,
                });
            }
        }

        return new MeterReadingFillResponseDto { Lines = result };
    }
}
