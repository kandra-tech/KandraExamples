using Kandra.Application;
using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Services;
using Kandra.Persistence;
using FamilyGarage.Application;
using FamilyGarage.Domain.Identity;
using FamilyGarage.Persistence;
using FamilyGarage.Persistence.Contexts;
using FamilyGarage.Persistence.Databases.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyGarage.Tests.Support;

/// <summary>
/// The configuration's real services on a SQLite in-memory database with the checked-in migrations applied, no web host:
/// what a behavior, a report or an import package needs to be exercised end to end. Every test builds its own host and
/// disposes it (the in-memory database dies with its one open connection). Resolve what you test inside
/// <see cref="InScopeAsync{T}"/>, one fresh scope per step: the generated CRUD services (<c>IDictionaryCrudService&lt;TDto, TQueryDto&gt;</c>,
/// <c>IDocumentCrudService&lt;TDto, TQueryDto&gt;</c>, <c>IReportService&lt;TFilters, TResult&gt;</c>), the register readers
/// (<c>IBalanceReader&lt;,&gt;</c>, <c>ITurnoverReader&lt;&gt;</c>, <c>IInfoRegisterReader&lt;&gt;</c>) and the constants services.
/// Callers are allowed everything. The caller's time zone is UTC unless the test passes another one: a rule about calendar
/// days or months needs a test east of UTC (<see cref="Kyiv"/>), where a document dated the 1st at local midnight is still
/// the previous month in UTC. See the test-configuration skill.
/// </summary>
public sealed class FamilyGarageTestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>Europe/Kyiv (UTC+2/+3), a zone east of UTC.</summary>
    public static TimeZoneInfo Kyiv { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");

    /// <summary>The UTC instant of local midnight of a day in <paramref name="zone"/>: what the client sends for a picked date.</summary>
    public static DateTime LocalMidnight(TimeZoneInfo zone, int year, int month, int day)
        => TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified), zone);

    public FamilyGarageTestHost(Action<IServiceCollection>? configure = null, TimeZoneInfo? timeZone = null)
    {
        var zone = timeZone ?? TimeZoneInfo.Utc;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<FamilyGarageDbContextSqlite>().UseSqlite(_connection).Options;

        using (var init = new FamilyGarageDbContextSqlite(options))
        {
            init.Database.Migrate();
        }

        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddScoped<DbContext>(_ => new FamilyGarageDbContextSqlite(options));
        services.AddScoped<FamilyGarageDbContext>(p => (FamilyGarageDbContext)p.GetRequiredService<DbContext>());
        services.AddScoped<IAuthorizationChecker>(_ => new AllowAll());
        services.AddScoped<ICurrentTimeZone>(_ => new FixedTimeZone(zone));
        services.AddScoped<ICallerContext>(_ => new TestCallerContext(zone.Id));
        services.AddSingleton<IReportResultCache, NullReportResultCache>();
        services.AddIdentityCore<DomainUser>().AddRoles<DomainRole>().AddEntityFrameworkStores<FamilyGarageDbContext>();
        services.AddKandraPersistence<DomainUser, DomainRole>(configuration);
        services.AddFamilyGaragePersistence();
        services.AddKandraApplication<DomainUser, DomainRole>();
        services.AddFamilyGarageApplication();
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

    private sealed class FixedTimeZone(TimeZoneInfo zone) : ICurrentTimeZone
    {
        public TimeZoneInfo? TimeZone => zone;
    }

    private sealed class TestCallerContext(string timeZoneId) : ICallerContext
    {
        public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public CallerUser? User => new CallerUser(TestUserId, "testuser", []);
        public string? TimeZoneId => timeZoneId;
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
