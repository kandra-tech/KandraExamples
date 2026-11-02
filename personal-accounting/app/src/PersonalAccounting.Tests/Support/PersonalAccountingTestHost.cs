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
/// Callers are allowed everything. The caller is a test user (<see cref="TestUserId"/>): <c>INotificationService.NotifyCallerAsync</c>
/// sends to it. The caller's time zone is UTC unless the test passes another one: a rule about calendar
/// days or months (a monthly budget, a dashboard's "this month") needs a test in <see cref="EastOfUtc"/>, where a local
/// day starts on the previous UTC day. See the test-configuration skill.
/// </summary>
public sealed class PersonalAccountingTestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>The id of the user every call runs as (the caller of a notification).</summary>
    public static Guid TestUserId { get; } = new("11111111-1111-1111-1111-111111111111");

    /// <summary>Kyiv (UTC+2, UTC+3 in summer): local midnight of a date is 21:00 or 22:00 UTC of the day before.</summary>
    public static TimeZoneInfo EastOfUtc { get; } =
        TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Kyiv", out var kyiv) ? kyiv : TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");

    /// <summary>Builds the host.</summary>
    /// <param name="configure">Replaces or adds services after the configuration's own registrations.</param>
    /// <param name="timeZone">The caller's time zone, as <c>ICurrentTimeZone</c> reports it; UTC when omitted.</param>
    public PersonalAccountingTestHost(Action<IServiceCollection>? configure = null, TimeZoneInfo? timeZone = null)
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
        services.AddScoped<ICurrentTimeZone>(_ => new FixedTimeZone(timeZone ?? TimeZoneInfo.Utc));
        services.AddScoped<ICallerContext>(_ => new TestCaller((timeZone ?? TimeZoneInfo.Utc).Id));
        // The notification service resolves users through Identity, which the web host registers and this host does not.
        services.AddIdentityCore<DomainUser>().AddRoles<DomainRole>().AddEntityFrameworkStores<PersonalAccountingDbContext>();
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

    private sealed class FixedTimeZone(TimeZoneInfo zone) : ICurrentTimeZone
    {
        public TimeZoneInfo? TimeZone => zone;
    }

    private sealed class TestCaller(string timeZoneId) : ICallerContext
    {
        public CallerUser? User => new(TestUserId, "test", []);
        public string? TimeZoneId => timeZoneId;
    }

    /// <summary>The UTC instant of local midnight of a calendar date in <paramref name="zone"/>: what the client sends for a date picked there.</summary>
    /// <param name="zone">The user's time zone.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <returns>The UTC instant.</returns>
    public static DateTime LocalMidnight(TimeZoneInfo zone, int year, int month, int day) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified), zone);

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
