using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Card #14: <c>transaction_revisions</c> mirrors <c>transactions</c> minus the generated/identity
/// plumbing (no <c>UpdatedAt</c>/<c>IsDeleted</c>/<c>DeletedAt</c>, no per-status stamps); its
/// <c>CreatedAt</c> doubles as the Revision-stage submission timestamp. Free-text fields are
/// <c>varchar(256)</c> (see CLAUDE.md > Code Conventions).
/// </summary>
public sealed class TransactionRevisionConfiguration : IEntityTypeConfiguration<TransactionRevision>
{
    public void Configure(EntityTypeBuilder<TransactionRevision> builder)
    {
        builder.ToTable("transaction_revisions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount)
            .HasColumnType("numeric(14,2)")
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(Transaction.FreeTextMaxLength)
            .IsRequired();

        builder.Property(r => r.ExpectedDate).HasColumnType("date").IsRequired();
        builder.Property(r => r.ActualDate).HasColumnType("date");
        builder.Property(r => r.ReconciledDate).HasColumnType("date");
        builder.Property(r => r.InvoiceDueDate).HasColumnType("date");

        // Reused as the Revision-stage submission timestamp — there is no separate SubmittedForReviewAt here.
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz").IsRequired();

        builder.HasOne(r => r.Transaction)
            .WithMany()
            .HasForeignKey(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Category)
            .WithMany()
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.TransactionId)
            .HasDatabaseName("ix_transaction_revisions_transaction_id");
    }
}
