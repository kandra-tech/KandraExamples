using FluentValidation;
using Kandra.Localization;
using PersonalAccounting.ClientLib.Common.ApiClients;
using PersonalAccounting.Forms.DataProcessors;
using Microsoft.Extensions.DependencyInjection;

// GeneratedPersonalAccountingPolicyBuilder lives directly in this project's own root namespace (see
// Kandra.Generators.ClientLib.PolicyGenerator - it names generated classes after the RootNamespace MSBuild
// property), so no extra using is needed for it.

namespace PersonalAccounting.ClientLib.Common;

public static class ClientPersonalAccountingServicesExtensions
{
    public static IServiceCollection ConfigurePersonalAccountingClientServices(this IServiceCollection services, Uri baseAddress)
    {
        // Every generated form injects its validator (FluentValidator<TValidator>), so each *Validator of the Forms
        // project must be in the client container too - one call covers the ones you add later. Without it the
        // backend builds and runs fine but opening a form in the browser crashes with "Cannot provide a value for
        // property 'validator'". (CompanySettingsValidator only names the Forms assembly: any type from it works.)
        services.AddValidatorsFromAssembly(typeof(CompanySettingsValidator).Assembly);

        // Server module registered first by Kandra.ClientLib.MudBlazor.ConfigureClientServices (called
        // before this method, from PersonalAccounting.Client.MudBlazor/Program.cs); registering this
        // configuration's own module afterward - as the more-recently-registered module - lets its keys
        // (e.g. BrandName) override the platform's for the same untyped IStringLocalizer generic RCL pages
        // (MainLayout/Index) inject.
        services.AddLocalizationModule<PersonalAccounting.Localization.PersonalAccounting>();

        // Registers every closed Refit client, bridges each to its generic client-interface, and
        // registers the Service-layer wrapper (Kandra.Generators.ClientLib.Clients.ClientGenerator).
        // That generator emits Add{Config}ClientServices only once there is at least one [Kandra*Form] Dto
        // or Handler in the compilation, which the CompanySettings data processor provides.
        services.AddPersonalAccountingClientServices(baseAddress);

        // Guid TypeId -> Dto/enum Type + Dictionary/Document/Enum kind, for client-side dynamic UI
        // dispatch - generated from every [TypeId]-attributed Dto/enum discoverable in this project's
        // compilation (Kandra.Generators.TypeRegistry) - a no-op today.
        services.AddGeneratedPersonalAccountingTypeRegistry();

        // Per-entity Insert/Update/Delete/View/Execute authorization policies - generated from every
        // [Kandra*Form]-attributed Dto by Kandra.Generators.ClientLib.PolicyGenerator - a no-op today.
        services.AddAuthorizationCore(options => GeneratedPersonalAccountingPolicyBuilder.Build(options));

        return services;
    }
}
