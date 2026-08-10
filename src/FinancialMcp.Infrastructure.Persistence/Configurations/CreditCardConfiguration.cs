using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures only the CreditCard-specific columns/relationship. Table, key, and the
/// soft-delete query filter are all inherited from AccountConfiguration via EF Core TPH
/// (CreditCard shares the "accounts" table with Account, distinguished by a discriminator).
/// </summary>
public sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.HasOne(c => c.PaymentAccount)
            .WithMany(a => a.CreditCards)
            .HasForeignKey(c => c.PaymentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
