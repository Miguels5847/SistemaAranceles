namespace SistemaAranceles.Domain.Common;

public sealed class DominioException : Exception
{
    public DominioException(string mensaje)
        : base(mensaje)
    {
    }
}
