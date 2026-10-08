namespace SistemaInventarioVentas.Application.Common;

public sealed record ApplicationError(ApplicationErrorCode Code, string Message);
