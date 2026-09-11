using Microsoft.EntityFrameworkCore;
using TaladPOS.Domain.Entities;

namespace TaladPOS.Infrastructure.Persistence;

public class TaladPOSDbContext(DbContextOptions<TaladPOSDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<StaffAuditLog> StaffAuditLogs => Set<StaffAuditLog>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaladPOSDbContext).Assembly);
    }
}
