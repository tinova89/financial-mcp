using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ver CLAUDE.md > Persistência (Postgres): Valor como numeric, datas como
/// date/timestamptz, índices para acelerar get_budget_status/get_balance_projection.
/// </summary>
public sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("transacoes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Valor)
            .HasColumnType("numeric(14,2)")
            .IsRequired();

        builder.Property(t => t.CategoriaBruta)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.DataPrevista).HasColumnType("date").IsRequired();
        builder.Property(t => t.DataEfetiva).HasColumnType("date");
        builder.Property(t => t.DataConciliado).HasColumnType("date");
        builder.Property(t => t.VencimentoFatura).HasColumnType("date");

        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(t => t.DeletedAt).HasColumnType("timestamptz");

        builder.HasOne(t => t.Conta)
            .WithMany(c => c.Transacoes)
            .HasForeignKey(t => t.ContaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Cartao)
            .WithMany(c => c.Transacoes)
            .HasForeignKey(t => t.CartaoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Acelera get_budget_status (agregação por categoria-mãe/mês).
        builder.HasIndex(t => new { t.Status, t.Tipo })
            .HasDatabaseName("ix_transacoes_status_tipo");

        builder.HasIndex(t => t.DataConciliado)
            .HasDatabaseName("ix_transacoes_data_conciliado");

        // Acelera get_balance_projection (parcelamento/ciclo de fatura por cartão).
        builder.HasIndex(t => new { t.CartaoId, t.ParcelaTotal })
            .HasDatabaseName("ix_transacoes_cartao_parcelatotal");

        builder.HasIndex(t => t.VencimentoFatura)
            .HasDatabaseName("ix_transacoes_venc_fatura");
    }
}
