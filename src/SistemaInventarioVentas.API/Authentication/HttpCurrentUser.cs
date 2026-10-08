using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SistemaInventarioVentas.Application.Interfaces;

namespace SistemaInventarioVentas.API.Authentication;

internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        IsAuthenticated &&
        Guid.TryParse(Principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : null;

    public IReadOnlyCollection<string> Roles =>
        IsAuthenticated
            ? Principal.FindAll("role").Select(claim => claim.Value).Distinct(StringComparer.Ordinal).ToArray()
            : [];
}
