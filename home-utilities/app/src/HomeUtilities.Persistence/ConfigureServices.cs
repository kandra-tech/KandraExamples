using Kandra.Domain.Commons;
using Kandra.Domain.Registers;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Registers;
using HomeUtilities.Domain.Commons;
using HomeUtilities.Domain.Entities.Registers;
using HomeUtilities.Persistence.Documents;
using HomeUtilities.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HomeUtilities.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection AddHomeUtilitiesPersistence(this IServiceCollection services)
    {
        // Entity repositories + registers: generated from the Kandra*Entity attributes by
        // Kandra.Generators.Di.Persistence - a no-op today since this configuration has none yet.
        services.AddGeneratedHomeUtilitiesPersistenceEntities();

        services.AddTurnoverRegister<UtilityCostMovement, UtilityCostDimensions, UtilityCostResources, NoDetails>();

        // Document Links / IDocumentReaderService's Guid->Type resolution - the one configuration-specific
        // piece those engine services depend on, same posture as HomeUtilitiesChartOfAccountsBuilder.
        services.AddSingleton<IDocumentTypeResolver, HomeUtilitiesDocumentTypeResolver>();

        // Hand-written, bespoke report queries - not an engine contract, just this configuration's own
        // placeholder for where that kind of query lives. Empty until the first report needs one.
        services.AddScoped<IReportsRepository, HomeUtilitiesReportsRepository>();

        return services;
    }
}
