using SistemaInventarioVentas.Application.Interfaces;

namespace SistemaInventarioVentas.Application.Tests;

internal sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
