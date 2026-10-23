namespace SistemaInventarioVentas.Application.Exceptions;

public class DuplicateSupplierIdentificationException : Exception
{
    public DuplicateSupplierIdentificationException(string message) : base(message)
    {
    }

    public DuplicateSupplierIdentificationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
