using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Scope).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.DiscountPercent).HasPrecision(5, 2);

        // Shadow relationship: Promotion.ProductId is only set when Scope = PerProduct;
        // no navigation property is exposed on either side.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
