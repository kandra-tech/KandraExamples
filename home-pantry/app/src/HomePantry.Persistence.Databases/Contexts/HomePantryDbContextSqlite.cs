using HomePantry.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HomePantry.Persistence.Databases.Contexts;

public class HomePantryDbContextSqlite : HomePantryDbContext
{
    public HomePantryDbContextSqlite(DbContextOptions<HomePantryDbContextSqlite> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // here you can customize database-specific model aspects
    }
}
