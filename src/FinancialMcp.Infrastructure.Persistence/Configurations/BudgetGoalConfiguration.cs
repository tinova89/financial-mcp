using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

public sealed class BudgetGoalConfiguration : IEntityTypeConfiguration<BudgetGoal>
{
    public void Configure(EntityTypeBuilder<BudgetGoal> builder)
    {
        builder.ToTable("budget_goals");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RawCategory).HasMaxLength(200).IsRequired();
        builder.Property(m => m.BudgetAmount).HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.DeletedAt).HasColumnType("timestamptz");

        // One goal per category/month.
        builder.HasIndex(m => new { m.RawCategory, m.Year, m.Month })
            .IsUnique()
            .HasDatabaseName("ux_budget_goals_category_year_month");
    }
}
