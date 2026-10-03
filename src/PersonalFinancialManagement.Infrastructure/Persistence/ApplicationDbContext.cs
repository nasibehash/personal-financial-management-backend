using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Common;

namespace PersonalFinancialManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add a DbSet<T> here for each entity, e.g.:
    // public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
