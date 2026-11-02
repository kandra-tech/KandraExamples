using Kandra.Domain.Commons;
using HomeUtilities.Domain.Entities.Documents;

namespace HomeUtilities.Persistence.Documents;

public sealed class HomeUtilitiesDocumentTypeResolver : IDocumentTypeResolver
{
    public Type? Resolve(Guid documentTypeId)
    {
        if (documentTypeId == SetTariffs.TypeId) return typeof(SetTariffs);
        if (documentTypeId == MeterReading.TypeId) return typeof(MeterReading);
        if (documentTypeId == Invoice.TypeId) return typeof(Invoice);
        if (documentTypeId == Payment.TypeId) return typeof(Payment);
        return null;
    }
}
