using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaInventarioVentas.Application.Interfaces;
using SistemaInventarioVentas.Infrastructure.Repositories;
using SistemaInventarioVentas.Infrastructure.Security;

namespace SistemaInventarioVentas.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        return services;
    }
}
