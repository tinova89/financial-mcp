using FinancialMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinancialMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// DescriptionCategoryMapping rows are only ever created/updated as a side effect of
/// processing a transaction (see IDescriptionCategoryMappingRecorder) — there is no
/// dedicated create/update MCP tool for this table.
/// </summary>
public sealed class DescriptionCategoryMappingConfiguration : IEntityTypeConfiguration<DescriptionCategoryMapping>
{
    public void Configure(EntityTypeBuilder<DescriptionCategoryMapping> builder)
    {
        builder.ToTable("description_category_mappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Description).HasMaxLength(500).IsRequired();

        builder.Property(m => m.CreatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(m => m.DeletedAt).HasColumnType("timestamptz");

        builder.HasOne(m => m.Category)
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One learned mapping per description — backs lookup_category and the
        // get-or-update upsert in IDescriptionCategoryMappingRecorder.
        builder.HasIndex(m => m.Description)
            .IsUnique()
            .HasDatabaseName("ux_description_category_mappings_description");
    }
}
