using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
