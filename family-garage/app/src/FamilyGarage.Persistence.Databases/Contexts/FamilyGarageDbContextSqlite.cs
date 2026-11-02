using FamilyGarage.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Persistence.Databases.Contexts;

public class FamilyGarageDbContextSqlite : FamilyGarageDbContext
{
    public FamilyGarageDbContextSqlite(DbContextOptions<FamilyGarageDbContextSqlite> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // here you can customize database-specific model aspects
    }
}
