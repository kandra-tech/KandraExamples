using Kandra.Domain.Commons;
using FamilyGarage.Domain.Entities.Documents;

namespace FamilyGarage.Persistence.Documents;

public sealed class FamilyGarageDocumentTypeResolver : IDocumentTypeResolver
{
    public Type? Resolve(Guid documentTypeId)
    {
        if (documentTypeId == VehiclePurchase.TypeId) return typeof(VehiclePurchase);
        if (documentTypeId == FinancePayment.TypeId) return typeof(FinancePayment);
        if (documentTypeId == VehicleSale.TypeId) return typeof(VehicleSale);
        if (documentTypeId == Refuel.TypeId) return typeof(Refuel);
        if (documentTypeId == Service.TypeId) return typeof(Service);
        if (documentTypeId == Accident.TypeId) return typeof(Accident);
        if (documentTypeId == Policy.TypeId) return typeof(Policy);
        return null;
    }
}
