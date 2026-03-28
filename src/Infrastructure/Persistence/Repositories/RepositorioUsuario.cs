using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;
using UsuarioDominio = SistemaAranceles.Domain.Entities.Usuario;
using UsuarioPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.Usuario;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioUsuario(
    ContextoAplicacion contextoAplicacion,
    IRepositorioGenerico<UsuarioPersistencia> repositorioGenerico)
    : IRepositorioUsuario
{
    public async Task<IReadOnlyList<UsuarioDominio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return usuarios.Select(MapearADominio).ToList();
    }

    public async Task<UsuarioDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioGenerico.ObtenerPorLlaveAsync([id], cancellationToken);
        return usuario is null ? null : MapearADominio(usuario);
    }

    public async Task<UsuarioDominio?> ObtenerPorCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default)
    {
        var correo = correoInstitucional.Trim().ToLowerInvariant();

        var usuario = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CorreoInstitucional == correo, cancellationToken);

        return usuario is null ? null : MapearADominio(usuario);
    }

    public async Task<bool> ExisteCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default)
    {
        var correo = correoInstitucional.Trim().ToLowerInvariant();
        return await contextoAplicacion.Usuarios.AnyAsync(x => x.CorreoInstitucional == correo, cancellationToken);
    }

    public async Task AgregarAsync(UsuarioDominio usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        await repositorioGenerico.AgregarAsync(MapearAPersistencia(usuario), cancellationToken);
    }

    public async Task ActualizarAsync(UsuarioDominio usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var existente = await contextoAplicacion.Usuarios
            .FirstOrDefaultAsync(x => x.Id == usuario.Id, cancellationToken);

        if (existente is null)
        {
            throw new KeyNotFoundException("No se encontro el usuario a actualizar.");
        }

        existente.NombreCompleto = usuario.NombreCompleto;
        existente.CorreoInstitucional = usuario.CorreoInstitucional.ToString();
        existente.HashContrasena = usuario.HashContrasena;
        existente.Estado = usuario.Estado.ToString();
        existente.UltimoAccesoEn = usuario.UltimoAccesoEn;
    }

    private static UsuarioDominio MapearADominio(UsuarioPersistencia entidad)
    {
        var usuario = new UsuarioDominio(
            entidad.NombreCompleto,
            new CorreoInstitucional(entidad.CorreoInstitucional),
            entidad.HashContrasena);

        usuario.RehidratarId(entidad.Id);

        if (Enum.TryParse<EstadoUsuario>(entidad.Estado, true, out var estado))
        {
            switch (estado)
            {
                case EstadoUsuario.Suspendido:
                    usuario.Suspender();
                    break;
                case EstadoUsuario.Inactivo:
                    usuario.Inactivar();
                    break;
                default:
                    usuario.Reactivar();
                    break;
            }
        }

        if (entidad.UltimoAccesoEn.HasValue)
        {
            usuario.RegistrarAcceso(entidad.UltimoAccesoEn.Value);
        }

        return usuario;
    }

    private static UsuarioPersistencia MapearAPersistencia(UsuarioDominio dominio)
    {
        return new UsuarioPersistencia
        {
            NombreCompleto = dominio.NombreCompleto,
            CorreoInstitucional = dominio.CorreoInstitucional.ToString(),
            HashContrasena = dominio.HashContrasena,
            Estado = dominio.Estado.ToString(),
            UltimoAccesoEn = dominio.UltimoAccesoEn
        };
    }
}
