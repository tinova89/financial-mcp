using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>Dedicated table for refresh tokens — never reuse the users table (see CLAUDE.md > Authentication).</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(r => r.RevokedAt).HasColumnType("timestamptz");
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}
