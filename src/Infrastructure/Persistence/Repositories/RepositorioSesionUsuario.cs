using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioSesionUsuario(ContextoAplicacion contextoAplicacion) : IRepositorioSesionUsuario
{
    public async Task<int> CrearAsync(
        int usuarioId,
        string tokenSesion,
        DateTime expiraEn,
        CancellationToken cancellationToken = default)
    {
        var sesion = new SesionUsuario
        {
            UsuarioId = usuarioId,
            TokenSesion = tokenSesion,
            EmitidoEn = DateTime.UtcNow,
            ExpiraEn = expiraEn
        };

        await contextoAplicacion.SesionesUsuario.AddAsync(sesion, cancellationToken);
        await contextoAplicacion.SaveChangesAsync(cancellationToken);
        return sesion.Id;
    }

    public async Task RevocarAsync(string tokenSesion, CancellationToken cancellationToken = default)
    {
        var sesion = await contextoAplicacion.SesionesUsuario
            .FirstOrDefaultAsync(s => s.TokenSesion == tokenSesion, cancellationToken);

        if (sesion is not null)
        {
            sesion.RevocadoEn = DateTime.UtcNow;
        }
    }

    public async Task RevocarTodasDelUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var sesiones = await contextoAplicacion.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && s.RevocadoEn == null)
            .ToListAsync(cancellationToken);

        var ahora = DateTime.UtcNow;
        foreach (var s in sesiones)
            s.RevocadoEn = ahora;
    }
}
