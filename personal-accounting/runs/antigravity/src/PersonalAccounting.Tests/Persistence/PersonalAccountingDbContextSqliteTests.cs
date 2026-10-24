using PersonalAccounting.Persistence.Databases.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PersonalAccounting.Tests.Persistence;

/// <summary>
/// Smoke test: the checked-in Migrations/Sqlite/* apply cleanly against a fresh SQLite in-memory
/// database. Keeps the connection open for the whole test (SQLite in-memory is destroyed the instant
/// its one open connection closes), matching the platform's documented testing convention.
/// </summary>
public sealed class PersonalAccountingDbContextSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PersonalAccountingDbContextSqlite _context;

    public PersonalAccountingDbContextSqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PersonalAccountingDbContextSqlite>()
            .UseSqlite(_connection)
            .Options;
        _context = new PersonalAccountingDbContextSqlite(options);
    }

    [Fact]
    public void Migrate_AppliesAllMigrations_WithoutError()
    {
        _context.Database.Migrate();

        var applied = _context.Database.GetAppliedMigrations();
        Assert.NotEmpty(applied);
        Assert.Empty(_context.Database.GetPendingMigrations());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
