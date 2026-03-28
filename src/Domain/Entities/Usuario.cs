using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;

namespace SistemaAranceles.Domain.Entities;

public sealed class Usuario : EntidadDominioBase
{
    private readonly List<Rol> _roles = new();

    private Usuario()
    {
    }

    public Usuario(string nombreCompleto, CorreoInstitucional correoInstitucional, string hashContrasena)
    {
        CambiarNombre(nombreCompleto);
        CorreoInstitucional = correoInstitucional;
        CambiarHashContrasena(hashContrasena);
        Estado = EstadoUsuario.Activo;
    }

    public string NombreCompleto { get; private set; } = string.Empty;
    public CorreoInstitucional CorreoInstitucional { get; private set; }
    public string HashContrasena { get; private set; } = string.Empty;
    public EstadoUsuario Estado { get; private set; }
    public DateTime? UltimoAccesoEn { get; private set; }
    public IReadOnlyCollection<Rol> Roles => _roles.AsReadOnly();

    public void CambiarNombre(string nombreCompleto)
    {
        NombreCompleto = GuardiaDominio.Requerido(nombreCompleto, "Nombre completo", 150);
    }

    public void CambiarCorreo(CorreoInstitucional correoInstitucional)
    {
        CorreoInstitucional = correoInstitucional;
    }

    public void CambiarHashContrasena(string hashContrasena)
    {
        HashContrasena = GuardiaDominio.Requerido(hashContrasena, "Hash de contrasena", 200);
    }

    public void RegistrarAcceso(DateTime fechaAcceso)
    {
        UltimoAccesoEn = fechaAcceso;
    }

    public void Suspender() => Estado = EstadoUsuario.Suspendido;

    public void Reactivar() => Estado = EstadoUsuario.Activo;

    public void Inactivar() => Estado = EstadoUsuario.Inactivo;

    public void AsignarRol(Rol rol)
    {
        if (rol is null)
        {
            throw new DominioException("El rol a asignar no puede ser nulo.");
        }

        if (_roles.Any(x => x.Nombre.Equals(rol.Nombre, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _roles.Add(rol);
    }

    public void QuitarRol(string nombreRol)
    {
        var rol = _roles.FirstOrDefault(x => x.Nombre.Equals(nombreRol, StringComparison.OrdinalIgnoreCase));
        if (rol is not null)
        {
            _roles.Remove(rol);
        }
    }
}
