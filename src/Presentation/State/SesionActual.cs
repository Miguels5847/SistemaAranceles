namespace SistemaAranceles.Presentation.State;

public sealed class SesionActual
{
    public bool EstaAutenticado { get; private set; }
    public int UsuarioId { get; private set; }
    public string NombreCompleto { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string RolNombre { get; private set; } = string.Empty;
    public string TokenSesion { get; private set; } = string.Empty;

    public bool EsAdministrador => RolNombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
    public bool EsAnalista => RolNombre.Equals("Analista", StringComparison.OrdinalIgnoreCase);
    public bool EsVisualizador => RolNombre.Equals("Visualizador", StringComparison.OrdinalIgnoreCase);

    public void IniciarSesion(int usuarioId, string nombreCompleto, string correo, string rolNombre, string tokenSesion)
    {
        UsuarioId = usuarioId;
        NombreCompleto = nombreCompleto;
        Correo = correo;
        RolNombre = rolNombre;
        TokenSesion = tokenSesion;
        EstaAutenticado = true;
    }

    public void CerrarSesion()
    {
        UsuarioId = 0;
        NombreCompleto = string.Empty;
        Correo = string.Empty;
        RolNombre = string.Empty;
        TokenSesion = string.Empty;
        EstaAutenticado = false;
    }
}
