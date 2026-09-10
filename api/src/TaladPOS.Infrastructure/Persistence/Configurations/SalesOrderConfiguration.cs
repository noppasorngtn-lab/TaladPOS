using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.PromotionDiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.MemberDiscountAmount).HasPrecision(18, 2);
        builder.Property(o => o.NetTotal).HasPrecision(18, 2);

        // Shadow relationships: StaffId/MemberId are plain FK columns, no navigation properties.
        builder.HasOne<Staff>().WithMany().HasForeignKey(o => o.StaffId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(o => o.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lines is a read-only wrapper over the private _lines backing field.
        builder.Navigation(o => o.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
