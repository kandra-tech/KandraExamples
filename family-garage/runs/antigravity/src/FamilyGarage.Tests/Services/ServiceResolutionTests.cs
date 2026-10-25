using Kandra.Application.Abstractions.Services;
using Kandra.Application.Services.DataProcessors;
using FamilyGarage.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyGarage.Tests.Services;

/// <summary>
/// Every generated CRUD, report and data-processor service of the configuration can be created. A dependency that is only
/// resolved when the service is first used - the classic one is the <c>{Name}QueryDtoValidator</c> (an
/// <c>IValidator&lt;{Name}QueryDto&gt;</c>) every dictionary and document needs - otherwise fails at runtime only, when somebody
/// opens the list ("No service for type 'FluentValidation.IValidator`1[...QueryDto]'"), while build and startup stay green.
/// </summary>
public sealed class ServiceResolutionTests : IDisposable
{
    private static readonly Type[] GeneratedServices =
    [
        typeof(IDictionaryCrudService<,>),
        typeof(IHierarchicalDictionaryCrudService<,>),
        typeof(IDocumentCrudService<,>),
        typeof(IReportService<,>),
        typeof(IDataProcessorService<,>),
    ];

    private readonly FamilyGarageTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void EveryGeneratedServiceResolves()
    {
        var serviceTypes = _host.Registrations
            .Select(d => d.ServiceType)
            .Where(t => t.IsGenericType && !t.ContainsGenericParameters && GeneratedServices.Contains(t.GetGenericTypeDefinition()))
            .Distinct()
            .ToList();

        Assert.NotEmpty(serviceTypes); // the template's own CompanySettings data processor is one

        var failures = new List<string>();
        using var scope = _host.CreateScope();
        foreach (var type in serviceTypes)
        {
            try
            {
                scope.ServiceProvider.GetRequiredService(type);
            }
            catch (Exception exception)
            {
                failures.Add($"{Describe(type)}: {exception.GetBaseException().Message}");
            }
        }

        Assert.True(failures.Count == 0, "Generated services that cannot be created:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }

    private static string Describe(Type type) =>
        $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>";
}
