using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        // Barcode is optional but unique when present (FR-014) — partial index skips NULLs.
        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("\"Barcode\" IS NOT NULL");

        // PostgreSQL has no native rowversion column; xmin backs optimistic concurrency
        // to prevent overselling under concurrent checkouts (research.md item 4).
        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");
    }
}
