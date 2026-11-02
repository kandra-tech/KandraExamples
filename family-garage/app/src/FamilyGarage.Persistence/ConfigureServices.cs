using Kandra.Domain.Commons;
using Kandra.Persistence;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Registers;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Domain.Entities.Registers;
using FamilyGarage.Persistence.Documents;
using FamilyGarage.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyGarage.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection AddFamilyGaragePersistence(this IServiceCollection services)
    {
        // Entity repositories + registers: generated from the Kandra*Entity attributes by
        // Kandra.Generators.Di.Persistence - a no-op today since this configuration has none yet.
        services.AddGeneratedFamilyGaragePersistenceEntities();

        // Turnover registers are not wired by the generator; register them manually:
        services.AddTurnoverRegister<VehicleCostsMovement, VehicleCostsDimensions, VehicleCostsResources, VehicleCostsDetails>();
        services.AddTurnoverRegister<ServiceLinesMovement, ServiceLinesDimensions, ServiceLinesResources, ServiceLinesDetails>();

        // Document Links / IDocumentReaderService's Guid->Type resolution - the one configuration-specific
        // piece those engine services depend on, same posture as FamilyGarageChartOfAccountsBuilder.
        services.AddSingleton<IDocumentTypeResolver, FamilyGarageDocumentTypeResolver>();

        // Hand-written, bespoke report queries - not an engine contract, just this configuration's own
        // placeholder for where that kind of query lives. Empty until the first report needs one.
        services.AddScoped<IReportsRepository, FamilyGarageReportsRepository>();

        return services;
    }
}
