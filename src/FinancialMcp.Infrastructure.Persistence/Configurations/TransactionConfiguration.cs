using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// See CLAUDE.md > Persistence (Postgres): Amount as numeric, dates as
/// date/timestamptz, indexes to speed up get_budget_status/get_balance_projection.
/// </summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasColumnType("numeric(14,2)")
            .IsRequired();

        builder.Property(t => t.RawCategory)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.ExpectedDate).HasColumnType("date").IsRequired();
        builder.Property(t => t.ActualDate).HasColumnType("date");
        builder.Property(t => t.ReconciledDate).HasColumnType("date");
        builder.Property(t => t.InvoiceDueDate).HasColumnType("date");

        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnType("timestamptz");

        builder.HasOne(t => t.Account)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreditCard)
            .WithMany()
            .HasForeignKey(t => t.CardId)
            .OnDelete(DeleteBehavior.Restrict);

        // Speeds up get_budget_status (aggregation by parent category/month).
        builder.HasIndex(t => new { t.Status, t.Type })
            .HasDatabaseName("ix_transactions_status_type");

        builder.HasIndex(t => t.ReconciledDate)
            .HasDatabaseName("ix_transactions_reconciled_date");

        // Speeds up get_balance_projection (installments/card statement cycle).
        builder.HasIndex(t => new { t.CardId, t.TotalInstallments })
            .HasDatabaseName("ix_transactions_card_total_installments");

        builder.HasIndex(t => t.InvoiceDueDate)
            .HasDatabaseName("ix_transactions_invoice_due_date");
    }
}
