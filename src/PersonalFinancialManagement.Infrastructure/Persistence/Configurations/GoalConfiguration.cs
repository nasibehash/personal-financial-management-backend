using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Infrastructure.Persistence.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");

        builder.Property(g => g.Name).IsRequired().HasMaxLength(150);
        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.TargetAmount).HasPrecision(18, 2);

        builder.HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.UserId, g.Status });
    }
}
