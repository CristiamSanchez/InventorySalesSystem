using Microsoft.Extensions.DependencyInjection;
using SistemaInventarioVentas.Application.Interfaces;

namespace SistemaInventarioVentas.Infrastructure.Security;

public static class JwtTokenServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAccessTokenIssuer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        return services;
    }
}
