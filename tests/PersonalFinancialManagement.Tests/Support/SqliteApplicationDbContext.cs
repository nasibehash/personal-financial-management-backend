using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Infrastructure.Persistence;

namespace PersonalFinancialManagement.Tests.Support;

// The real context targets SQL Server. SQLite cannot aggregate decimal columns, so the
// tests store decimals as doubles; everything else (relations, indexes) stays as configured.
public class SqliteApplicationDbContext : ApplicationDbContext
{
    public SqliteApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(decimal))
            .Select(property => (Entity: property.DeclaringType.ClrType, property.Name))
            .ToList();

        foreach (var (entity, name) in decimalProperties)
            modelBuilder.Entity(entity).Property(name).HasConversion<double>();
    }
}
