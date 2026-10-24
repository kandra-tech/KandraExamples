using Kandra.Application;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Services;
using Kandra.Persistence;
using PersonalAccounting.Application;
using PersonalAccounting.Domain.Identity;
using PersonalAccounting.Persistence;
using PersonalAccounting.Persistence.Contexts;
using PersonalAccounting.Persistence.Databases.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalAccounting.Tests.Support;

/// <summary>
/// The configuration's real services on a SQLite in-memory database with the checked-in migrations applied, no web host:
/// what a behavior, a report or an import package needs to be exercised end to end. Every test builds its own host and
/// disposes it (the in-memory database dies with its one open connection). Resolve what you test inside
/// <see cref="InScopeAsync{T}"/>, one fresh scope per step: the generated CRUD services (<c>IDictionaryCrudService&lt;TDto, TQueryDto&gt;</c>,
/// <c>IDocumentCrudService&lt;TDto, TQueryDto&gt;</c>, <c>IReportService&lt;TFilters, TResult&gt;</c>), the register readers
/// (<c>IBalanceReader&lt;,&gt;</c>, <c>ITurnoverReader&lt;&gt;</c>, <c>IInfoRegisterReader&lt;&gt;</c>) and the constants services.
/// Callers are allowed everything and the time zone is UTC. See the test-configuration skill.
/// </summary>
public sealed class PersonalAccountingTestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    public PersonalAccountingTestHost(Action<IServiceCollection>? configure = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<PersonalAccountingDbContextSqlite>().UseSqlite(_connection).Options;

        using (var init = new PersonalAccountingDbContextSqlite(options))
        {
            init.Database.Migrate();
        }

        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddScoped<DbContext>(_ => new PersonalAccountingDbContextSqlite(options));
        services.AddScoped<PersonalAccountingDbContext>(p => (PersonalAccountingDbContext)p.GetRequiredService<DbContext>());
        services.AddScoped<IAuthorizationChecker>(_ => new AllowAll());
        services.AddScoped<ICurrentTimeZone>(_ => new UtcTimeZone());
        services.AddSingleton<IReportResultCache, NullReportResultCache>();
        services.AddKandraPersistence<DomainUser, DomainRole>(configuration);
        services.AddPersonalAccountingPersistence();
        services.AddKandraApplication<DomainUser, DomainRole>();
        services.AddPersonalAccountingApplication();
        configure?.Invoke(services);

        Registrations = services.ToList();
        Services = services.BuildServiceProvider();
    }

    public ServiceProvider Services { get; }

    /// <summary>Every registration the host was built from (used by <c>ServiceResolutionTests</c>).</summary>
    public IReadOnlyList<ServiceDescriptor> Registrations { get; }

    public IServiceScope CreateScope() => Services.CreateScope();

    /// <summary>
    /// Runs one step in a fresh scope (its own DbContext and change tracker) and returns its result. Use one call per
    /// step of a test (insert, submit, read back, update ...): updating a document in the scope that inserted or read it
    /// fails with "another instance with the same key value is already being tracked".
    /// </summary>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> step)
    {
        using var scope = Services.CreateScope();
        return await step(scope.ServiceProvider);
    }

    /// <inheritdoc cref="InScopeAsync{T}"/>
    public async Task InScopeAsync(Func<IServiceProvider, Task> step)
    {
        using var scope = Services.CreateScope();
        await step(scope.ServiceProvider);
    }

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class AllowAll : IAuthorizationChecker
    {
        public void Authorize(IAuthorizationPoint point)
        {
        }

        public bool IsAdmin() => true;
        public bool IsHeadOfDepartment() => true;
        public Guid UserId() => Guid.Empty;
    }

    private sealed class UtcTimeZone : ICurrentTimeZone
    {
        public TimeZoneInfo? TimeZone => TimeZoneInfo.Utc;
    }

#pragma warning disable CS8633, CS8767
    private sealed class NullReportResultCache : IReportResultCache
    {
        public void Store<TResult>(Guid userId, string cacheTag, TResult result)
        {
        }

        public bool TryGet<TResult>(Guid userId, string? cacheTag, out TResult result)
        {
            result = default!;
            return false;
        }
    }
#pragma warning restore CS8633, CS8767
}
