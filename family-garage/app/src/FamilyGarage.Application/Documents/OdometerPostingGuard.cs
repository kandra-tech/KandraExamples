using Kandra.Application.Abstractions.Services;
using Kandra.Domain.Exceptions;
using Kandra.Persistence.Abstractions.Registers;
using FamilyGarage.Domain.Entities.Registers;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.Documents;

public static class OdometerPostingGuard
{
    public static async Task EnsureOdometerValidAsync(
        ISubmitScope registers,
        Guid vehicleId,
        DateTime documentDate,
        Guid documentId,
        int newOdometer,
        IStringLocalizer localizer,
        CancellationToken ct)
    {
        var reader = registers.GetReader<IInfoRegisterReader<OdometerRecord>>();
        var lastSlice = await reader.SliceOfLast(
            documentDate,
            q => q.Where(r => r.Dimensions.VehicleId == vehicleId && r.DocumentId != documentId),
            ct);

        var lastReading = lastSlice.FirstOrDefault();
        if (lastReading != null && newOdometer < lastReading.Details.Kilometres)
        {
            throw new InvalidStateException(localizer["Odometer_LowerThanPrevious", newOdometer, lastReading.Details.Kilometres].Value);
        }
    }
}
