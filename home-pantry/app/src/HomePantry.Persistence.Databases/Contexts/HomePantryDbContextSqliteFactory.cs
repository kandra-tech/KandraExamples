using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomePantry.Persistence.Databases.Contexts;

public class HomePantryDbContextSqliteFactory : IDesignTimeDbContextFactory<HomePantryDbContextSqlite>
{
    public HomePantryDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HomePantryDbContextSqlite>();

        optionsBuilder.UseApplicationServiceProvider(DesignTimeIdentityServices.BuildServiceProvider());
        optionsBuilder.UseSqlite("Data Source = ./db/homepantry.db", opts =>
        {
        });

        return new HomePantryDbContextSqlite(optionsBuilder.Options);
    }
}
