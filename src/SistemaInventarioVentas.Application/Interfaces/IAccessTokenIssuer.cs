using SistemaInventarioVentas.Application.Models;
using SistemaInventarioVentas.Domain.Entities;

namespace SistemaInventarioVentas.Application.Interfaces;

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
