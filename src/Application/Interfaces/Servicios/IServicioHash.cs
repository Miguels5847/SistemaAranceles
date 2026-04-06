namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioHash
{
    string Hashear(string textoPlano);
    bool Verificar(string textoPlano, string hash);
}
