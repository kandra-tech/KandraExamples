using Kandra.Domain.Commons;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Registers;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Persistence.Documents;
using PersonalAccounting.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalAccounting.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection AddPersonalAccountingPersistence(this IServiceCollection services)
    {
        // Entity repositories + registers: generated from the Kandra*Entity attributes by
        // Kandra.Generators.Di.Persistence - a no-op today since this configuration has none yet.
        services.AddGeneratedPersonalAccountingPersistenceEntities();

        // Turnover registers are registered manually (not covered by the DI generator)
        services.AddTurnoverRegister<CategoryTurnoverMovement, CategoryTurnoverDimensions, CategoryTurnoverResources, CategoryTurnoverDetails>();

        // Document Links / IDocumentReaderService's Guid->Type resolution - the one configuration-specific
        // piece those engine services depend on, same posture as PersonalAccountingChartOfAccountsBuilder.
        services.AddSingleton<IDocumentTypeResolver, PersonalAccountingDocumentTypeResolver>();

        // Hand-written, bespoke report queries - not an engine contract, just this configuration's own
        // placeholder for where that kind of query lives. Empty until the first report needs one.
        services.AddScoped<IReportsRepository, PersonalAccountingReportsRepository>();

        return services;
    }
}
