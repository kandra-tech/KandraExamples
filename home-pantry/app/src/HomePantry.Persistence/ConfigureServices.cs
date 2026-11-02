using Kandra.Domain.Commons;
using Kandra.Persistence.Abstractions;
using HomePantry.Domain.Commons;
using HomePantry.Persistence.Documents;
using HomePantry.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HomePantry.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection AddHomePantryPersistence(this IServiceCollection services)
    {
        // Entity repositories + registers: generated from the Kandra*Entity attributes by
        // Kandra.Generators.Di.Persistence - a no-op today since this configuration has none yet.
        services.AddGeneratedHomePantryPersistenceEntities();

        // Document Links / IDocumentReaderService's Guid->Type resolution - the one configuration-specific
        // piece those engine services depend on, same posture as HomePantryChartOfAccountsBuilder.
        services.AddSingleton<IDocumentTypeResolver, HomePantryDocumentTypeResolver>();

        // Hand-written, bespoke report queries - not an engine contract, just this configuration's own
        // placeholder for where that kind of query lives. Empty until the first report needs one.
        services.AddScoped<IReportsRepository, HomePantryReportsRepository>();

        return services;
    }
}
