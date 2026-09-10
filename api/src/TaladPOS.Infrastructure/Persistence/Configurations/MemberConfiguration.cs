using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(m => m.AccumulatedPurchaseTotal).HasPrecision(18, 2);

        // Phone number dedupe rule (FR-016).
        builder.HasIndex(m => m.PhoneNumber).IsUnique();
    }
}
