using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(l => l.UnitPriceSnapshot).HasPrecision(18, 2);
        builder.Property(l => l.LineDiscountAmount).HasPrecision(18, 2);

        // Kept even if the referenced Product is later soft-deleted (FR-011) — no cascade delete.
        builder.HasOne<Product>().WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
