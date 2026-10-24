using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonalAccounting.Persistence.Databases.Contexts;

public class PersonalAccountingDbContextSqliteFactory : IDesignTimeDbContextFactory<PersonalAccountingDbContextSqlite>
{
    public PersonalAccountingDbContextSqlite CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PersonalAccountingDbContextSqlite>();

        optionsBuilder.UseApplicationServiceProvider(DesignTimeIdentityServices.BuildServiceProvider());
        optionsBuilder.UseSqlite("Data Source = ./db/personalaccounting.db", opts =>
        {
        });

        return new PersonalAccountingDbContextSqlite(optionsBuilder.Options);
    }
}
