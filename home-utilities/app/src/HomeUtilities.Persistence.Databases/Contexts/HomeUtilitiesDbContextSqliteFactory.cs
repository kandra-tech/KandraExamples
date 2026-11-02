using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HomeUtilities.Persistence.Databases.Contexts;

public class HomeUtilitiesDbContextSqliteFactory : IDesignTimeDbContextFactory<HomeUtilitiesDbContextSqlite>
{
    public HomeUtilitiesDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HomeUtilitiesDbContextSqlite>();

        optionsBuilder.UseApplicationServiceProvider(DesignTimeIdentityServices.BuildServiceProvider());
        optionsBuilder.UseSqlite("Data Source = ./db/homeutilities.db", opts =>
        {
        });

        return new HomeUtilitiesDbContextSqlite(optionsBuilder.Options);
    }
}
