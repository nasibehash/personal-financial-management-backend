using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinancialManagement.Domain.Entities;

namespace PersonalFinancialManagement.Infrastructure.Persistence.Configurations;

public class GoalContributionConfiguration : IEntityTypeConfiguration<GoalContribution>
{
    public void Configure(EntityTypeBuilder<GoalContribution> builder)
    {
        builder.ToTable("GoalContributions");

        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Note).HasMaxLength(500);

        builder.HasOne(c => c.Goal)
            .WithMany(g => g.Contributions)
            .HasForeignKey(c => c.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.GoalId);
    }
}
