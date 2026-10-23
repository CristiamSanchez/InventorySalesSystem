namespace SistemaInventarioVentas.Application.Exceptions;

public class DuplicateCustomerIdentificationException : Exception
{
    public DuplicateCustomerIdentificationException(string message) : base(message)
    {
    }

    public DuplicateCustomerIdentificationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
