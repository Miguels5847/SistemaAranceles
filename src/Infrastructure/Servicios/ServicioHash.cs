using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed class ServicioHash : IServicioHash
{
    public string Hashear(string textoPlano)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textoPlano);
        return BCrypt.Net.BCrypt.HashPassword(textoPlano, workFactor: 11);
    }

    public bool Verificar(string textoPlano, string hash)
    {
        if (string.IsNullOrWhiteSpace(textoPlano) || string.IsNullOrWhiteSpace(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(textoPlano, hash);
    }
}
