using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// See CLAUDE.md > Persistence (Postgres): Amount as numeric, dates as
/// date/timestamptz, indexes to speed up get_budget_status.
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

        // RawCategory is transient support data (the raw statement input used to resolve
        // Category via ITransactionCategoryResolver) — never persisted. See CLAUDE.md >
        // Category and subcategory / Transaction.RawCategory doc comment.
        builder.Ignore(t => t.RawCategory);

        // Card #14: every free-text field on transactions is capped at 256 chars (varchar(256)).
        builder.Property(t => t.Description)
            .HasMaxLength(Domain.Entities.Transaction.FreeTextMaxLength)
            .IsRequired();

        builder.Property(t => t.ExpectedDate).HasColumnType("date").IsRequired();
        builder.Property(t => t.ActualDate).HasColumnType("date");
        builder.Property(t => t.ReconciledDate).HasColumnType("date");
        builder.Property(t => t.InvoiceDueDate).HasColumnType("date");

        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnType("timestamptz");

        // Card #14: per-status transition timestamps.
        builder.Property(t => t.SubmittedForReviewAt).HasColumnType("timestamptz");
        builder.Property(t => t.ScheduledAt).HasColumnType("timestamptz");
        builder.Property(t => t.ConfirmedAt).HasColumnType("timestamptz");

        builder.HasOne(t => t.Account)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.CategoryId)
            .HasDatabaseName("ix_transactions_category_id");

        // Speeds up get_budget_status (aggregation by parent category/month).
        builder.HasIndex(t => new { t.Status, t.Type })
            .HasDatabaseName("ix_transactions_status_type");

        builder.HasIndex(t => t.ReconciledDate)
            .HasDatabaseName("ix_transactions_reconciled_date");

        // Speeds up installment/card-statement-cycle lookups by account — AccountId
        // identifies the specific card for credit-card-sourced transactions too, since a
        // CreditCard's own id is what's stored there (see Transaction.AccountId doc).
        builder.HasIndex(t => new { t.AccountId, t.TotalInstallments })
            .HasDatabaseName("ix_transactions_account_total_installments");

        builder.HasIndex(t => t.InvoiceDueDate)
            .HasDatabaseName("ix_transactions_invoice_due_date");
    }
}
