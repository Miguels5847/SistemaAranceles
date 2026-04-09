namespace SistemaAranceles.Presentation.State;

public sealed class SesionActual
{
    private bool _permisosEfectivosCargados;

    public bool EstaAutenticado { get; private set; }
    public int UsuarioId { get; private set; }
    public string NombreCompleto { get; private set; } = string.Empty;
    public string Correo { get; private set; } = string.Empty;
    public string RolNombre { get; private set; } = string.Empty;
    public string TokenSesion { get; private set; } = string.Empty;

    public bool EsAdministrador => RolNombre.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
    public bool EsAnalista => RolNombre.Equals("Analista", StringComparison.OrdinalIgnoreCase);
    public bool EsVisualizador => RolNombre.Equals("Visualizador", StringComparison.OrdinalIgnoreCase);

    /// <summary>Códigos de permiso efectivos del usuario (puede estar vacío si la carga falló).</summary>
    public IReadOnlySet<string> PermisosEfectivos { get; private set; } = new HashSet<string>();

    /// <summary>
    /// Verifica si el usuario tiene un permiso por su código (ej. "US.VER").
    /// Si los permisos no pudieron cargarse, usa fallback por rol (EsAdministrador).
    /// </summary>
    public bool TienePermiso(string codigo)
    {
        // Permisos cargados y con contenido → usarlos
        if (_permisosEfectivosCargados && PermisosEfectivos.Count > 0)
            return PermisosEfectivos.Contains(codigo);

        // Fallback: si los permisos no cargaron (o la tabla permiso está vacía), usar rol
        return EsAdministrador;
    }

    public void IniciarSesion(int usuarioId, string nombreCompleto, string correo, string rolNombre, string tokenSesion)
    {
        UsuarioId = usuarioId;
        NombreCompleto = nombreCompleto;
        Correo = correo;
        RolNombre = rolNombre;
        TokenSesion = tokenSesion;
        EstaAutenticado = true;
    }

    /// <summary>Establece los permisos efectivos obtenidos al login.</summary>
    public void EstablecerPermisos(IReadOnlySet<string> permisos)
    {
        PermisosEfectivos = permisos;
        _permisosEfectivosCargados = true;
    }

    public void CerrarSesion()
    {
        UsuarioId = 0;
        NombreCompleto = string.Empty;
        Correo = string.Empty;
        RolNombre = string.Empty;
        TokenSesion = string.Empty;
        EstaAutenticado = false;
        PermisosEfectivos = new HashSet<string>();
        _permisosEfectivosCargados = false;
    }
}
