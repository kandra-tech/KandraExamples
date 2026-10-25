using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FamilyGarage.Persistence.Databases.Contexts;

public class FamilyGarageDbContextSqliteFactory : IDesignTimeDbContextFactory<FamilyGarageDbContextSqlite>
{
    public FamilyGarageDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FamilyGarageDbContextSqlite>();

        optionsBuilder.UseApplicationServiceProvider(DesignTimeIdentityServices.BuildServiceProvider());
        optionsBuilder.UseSqlite("Data Source = ./db/familygarage.db", opts =>
        {
        });

        return new FamilyGarageDbContextSqlite(optionsBuilder.Options);
    }
}
