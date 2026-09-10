using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;
using TaladPOS.Infrastructure.Persistence.Seed;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Username).IsRequired().HasMaxLength(100);
        builder.Property(s => s.PasswordHash).IsRequired();
        builder.Property(s => s.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Username).IsUnique();

        // Bootstraps the first login on a brand-new database (quickstart.md prerequisites).
        builder.HasData(AdminSeed.GetSeedData());
    }
}
