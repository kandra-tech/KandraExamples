using HomeUtilities.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HomeUtilities.Persistence.Databases.Contexts;

public class HomeUtilitiesDbContextSqlite : HomeUtilitiesDbContext
{
    public HomeUtilitiesDbContextSqlite(DbContextOptions<HomeUtilitiesDbContextSqlite> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // here you can customize database-specific model aspects
    }
}
