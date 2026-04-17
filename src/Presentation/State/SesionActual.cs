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
    /// 
    /// Lógica:
    /// 1) Si permisos se cargaron correctamente → verificar en el set.
    /// 2) Si permisos están vacíos pero se cargaron:
    ///    - Administrador → tiene todos (fallback true).
    ///    - Otros → no tienen (fallback false) para evitar mostrar menú vacío.
    /// 3) Si permisos NO se cargaron (timeout persistente) → fallback por rol (admin=true).
    /// </summary>
    public bool TienePermiso(string codigo)
    {
        // 1) Si permisos se cargaron y tienen contenido, usarlos
        if (_permisosEfectivosCargados && PermisosEfectivos.Count > 0)
            return PermisosEfectivos.Contains(codigo);

        // 2) Si permisos se cargaron pero están vacíos (ej. rol Visualizador sin permisos)
        if (_permisosEfectivosCargados && PermisosEfectivos.Count == 0)
        {
            // Solo admin puede acceder a todo cuando permisos están vacíos
            return EsAdministrador;
        }

        // 3) Si permisos NO se cargaron (timeout persistente), fallback por rol
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
