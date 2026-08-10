using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.BankCode).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Kind).HasConversion<int>();
        builder.Property(e => e.InitialAmount).HasPrecision(18, 2);
        builder.Property(e => e.BaseCurrencyCode).IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => e.BankCode);
    }
}
