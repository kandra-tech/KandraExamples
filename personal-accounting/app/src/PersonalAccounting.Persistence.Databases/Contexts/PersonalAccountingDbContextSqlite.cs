using PersonalAccounting.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace PersonalAccounting.Persistence.Databases.Contexts;

public class PersonalAccountingDbContextSqlite : PersonalAccountingDbContext
{
    public PersonalAccountingDbContextSqlite(DbContextOptions<PersonalAccountingDbContextSqlite> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // here you can customize database-specific model aspects
    }
}
