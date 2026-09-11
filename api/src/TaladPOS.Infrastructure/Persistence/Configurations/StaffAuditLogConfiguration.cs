using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence.Configurations;

public class StaffAuditLogConfiguration : IEntityTypeConfiguration<StaffAuditLog>
{
    public void Configure(EntityTypeBuilder<StaffAuditLog> builder)
    {
        builder.ToTable("StaffAuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);

        // No cascade delete — Staff accounts are only ever soft-disabled, never hard-deleted
        // (spec.md Assumptions), so the audit trail never loses its target.
        builder.HasOne<Staff>()
            .WithMany()
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Staff>()
            .WithMany()
            .HasForeignKey(a => a.PerformedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
