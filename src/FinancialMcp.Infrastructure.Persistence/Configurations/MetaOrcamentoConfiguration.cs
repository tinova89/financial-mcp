using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

public sealed class MetaOrcamentoConfiguration : IEntityTypeConfiguration<MetaOrcamento>
{
    public void Configure(EntityTypeBuilder<MetaOrcamento> builder)
    {
        builder.ToTable("metas_orcamento");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.CategoriaBruta).HasMaxLength(200).IsRequired();
        builder.Property(m => m.MetaValor).HasColumnType("numeric(14,2)").IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.DeletedAt).HasColumnType("timestamptz");

        // Uma meta por categoria/mês.
        builder.HasIndex(m => new { m.CategoriaBruta, m.Ano, m.Mes })
            .IsUnique()
            .HasDatabaseName("ux_metas_categoria_ano_mes");
    }
}
