namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

public sealed class Usuario : EntidadBase
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoInstitucional { get; set; } = string.Empty;
    public string HashContrasena { get; set; } = string.Empty;
    public string Estado { get; set; } = "Activo";
    public DateTime? UltimoAccesoEn { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<SesionUsuario> Sesiones { get; set; } = new List<SesionUsuario>();
}

public sealed class Rol : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public sealed class Permiso : EntidadBase
{
    public string Codigo { get; set; } = string.Empty;
    public string ModuloNombre { get; set; } = string.Empty;
    public string AccionNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}

public sealed class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }

    public Rol? Rol { get; set; }
    public Permiso? Permiso { get; set; }
}

public sealed class UsuarioRol
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}

public sealed class SesionUsuario
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string TokenSesion { get; set; } = string.Empty;
    public DateTime EmitidoEn { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEn { get; set; }
    public DateTime? RevocadoEn { get; set; }

    public Usuario? Usuario { get; set; }
}

public sealed class AuditoriaLog
{
    public int Id { get; set; }
    public DateTime EventoEn { get; set; } = DateTime.UtcNow;
    public string ModuloNombre { get; set; } = string.Empty;
    public string EntidadNombre { get; set; } = string.Empty;
    public string EntidadId { get; set; } = string.Empty;
    public string AccionNombre { get; set; } = string.Empty;
    public string ResumenTexto { get; set; } = string.Empty;
    public string? ValoresAnterioresJson { get; set; }
    public string? ValoresNuevosJson { get; set; }
    public int? EjecutadoPorUsuarioId { get; set; }

    public Usuario? EjecutadoPorUsuario { get; set; }
}

public sealed class Carrera : EntidadBase
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string FacultadNombre { get; set; } = string.Empty;
    public int TotalCiclos { get; set; }

    public ICollection<EscenarioProyeccion> EscenariosProyeccion { get; set; } = new List<EscenarioProyeccion>();
    public ICollection<CargoFacultad> CargosFacultad { get; set; } = new List<CargoFacultad>();
}

public sealed class PeriodoAcademico : EntidadBase
{
    public int Anio { get; set; }
    public int NumeroPeriodo { get; set; }
    public string EtiquetaPeriodo { get; set; } = string.Empty;
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
}

public sealed class EscenarioProyeccion : EntidadBase
{
    public int CarreraId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsPredeterminado { get; set; }

    public Carrera? Carrera { get; set; }

    public ICollection<InflacionProyectada> InflacionesProyectadas { get; set; } = new List<InflacionProyectada>();
    public ICollection<ConfiguracionRetencion> ConfiguracionesRetencion { get; set; } = new List<ConfiguracionRetencion>();
    public ICollection<ProyeccionEstudiantes> ProyeccionesEstudiantes { get; set; } = new List<ProyeccionEstudiantes>();
}

public sealed class InflacionAnual : EntidadBase
{
    public int Anio { get; set; }
    public decimal PorcentajeInflacion { get; set; }
    public string FuenteNombre { get; set; } = string.Empty;
    public string TipoFuente { get; set; } = string.Empty;
}

public sealed class InflacionProyectada : EntidadBase
{
    public int EscenarioProyeccionId { get; set; }
    public int Anio { get; set; }
    public decimal PorcentajeInflacion { get; set; }
    public string MetodoProyeccion { get; set; } = string.Empty;
    public bool EsAjusteManual { get; set; }

    public EscenarioProyeccion? EscenarioProyeccion { get; set; }
}

public sealed class ConfiguracionRetencion : EntidadBase
{
    public int CarreraId { get; set; }
    public int EscenarioProyeccionId { get; set; }
    public int TotalCiclos { get; set; }
    public decimal TasaRetencionPorcentaje { get; set; }
    public decimal TasaGraduacionPorcentaje { get; set; }
    public decimal EstudiantesPeriodo1 { get; set; }
    public decimal EstudiantesPeriodo2 { get; set; }
    public int ParalelosPeriodo1 { get; set; }
    public int ParalelosPeriodo2 { get; set; }

    public Carrera? Carrera { get; set; }
    public EscenarioProyeccion? EscenarioProyeccion { get; set; }
    public CriterioReferenciaRetencion? CriterioReferenciaRetencion { get; set; }
    public ICollection<SimulacionRetencion> SimulacionesRetencion { get; set; } = new List<SimulacionRetencion>();
}

public sealed class CriterioReferenciaRetencion : EntidadBase
{
    public int ConfiguracionRetencionId { get; set; }
    public decimal MetaRetencionPorcentaje { get; set; }
    public decimal MetaGraduacionPorcentaje { get; set; }

    public ConfiguracionRetencion? ConfiguracionRetencion { get; set; }
}

public sealed class SimulacionRetencion : EntidadBase
{
    public int ConfiguracionRetencionId { get; set; }
    public DateTime EjecutadoEn { get; set; } = DateTime.UtcNow;
    public string? Notas { get; set; }

    public ConfiguracionRetencion? ConfiguracionRetencion { get; set; }
    public ICollection<DetalleSimulacionRetencion> DetallesSimulacionRetencion { get; set; } = new List<DetalleSimulacionRetencion>();
}

public sealed class DetalleSimulacionRetencion : EntidadBase
{
    public int SimulacionRetencionId { get; set; }
    public int NumeroCiclo { get; set; }
    public int NumeroPeriodo { get; set; }
    public decimal ValorEstudiantes { get; set; }
    public decimal TasaAplicadaPorcentaje { get; set; }
    public string TipoZona { get; set; } = string.Empty;

    public SimulacionRetencion? SimulacionRetencion { get; set; }
}

public sealed class ProyeccionEstudiantes : EntidadBase
{
    public int CarreraId { get; set; }
    public int EscenarioProyeccionId { get; set; }
    public int AnioBase { get; set; }
    public int SemanasPorSemestre { get; set; }

    public Carrera? Carrera { get; set; }
    public EscenarioProyeccion? EscenarioProyeccion { get; set; }
    public ICollection<DetalleProyeccionEstudiantes> DetallesProyeccionEstudiantes { get; set; } = new List<DetalleProyeccionEstudiantes>();
    public ICollection<ConfiguracionCargaDocente> ConfiguracionesCargaDocente { get; set; } = new List<ConfiguracionCargaDocente>();
}

public sealed class DetalleProyeccionEstudiantes : EntidadBase
{
    public int ProyeccionEstudiantesId { get; set; }
    public int PeriodoAcademicoId { get; set; }
    public int NumeroCiclo { get; set; }
    public int CantidadParalelos { get; set; }
    public decimal TotalEstudiantes { get; set; }

    public ProyeccionEstudiantes? ProyeccionEstudiantes { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }
}

public sealed class ConfiguracionCargaDocente : EntidadBase
{
    public int ProyeccionEstudiantesId { get; set; }
    public decimal HorasDocenciaEstandar { get; set; }
    public decimal HorasTecnicoEstandar { get; set; }
    public decimal ProporcionPhdPorcentaje { get; set; }
    public decimal ProporcionMgsPorcentaje { get; set; }

    public ProyeccionEstudiantes? ProyeccionEstudiantes { get; set; }
    public ICollection<ProyeccionRequerimientoDocente> ProyeccionesRequerimientoDocente { get; set; } = new List<ProyeccionRequerimientoDocente>();
}

public sealed class ProyeccionRequerimientoDocente : EntidadBase
{
    public int ConfiguracionCargaDocenteId { get; set; }
    public int PeriodoAcademicoId { get; set; }
    public decimal TotalDocentes { get; set; }
    public decimal DocentesPhd { get; set; }
    public decimal DocentesMgs { get; set; }
    public decimal DocentesParcial { get; set; }
    public decimal DocentesTecnico { get; set; }

    public ConfiguracionCargaDocente? ConfiguracionCargaDocente { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }
}

public sealed class CargoFacultad : EntidadBase
{
    public int CarreraId { get; set; }
    public string NombreCargo { get; set; } = string.Empty;
    public string TipoCargo { get; set; } = string.Empty;
    public decimal SueldoBaseMensual { get; set; }
    public bool EsCargoDocente { get; set; }

    public Carrera? Carrera { get; set; }
    public ICollection<ProyeccionCargoFacultad> ProyeccionesCargoFacultad { get; set; } = new List<ProyeccionCargoFacultad>();
}

public sealed class ProyeccionCargoFacultad : EntidadBase
{
    public int CargoFacultadId { get; set; }
    public int PeriodoAcademicoId { get; set; }
    public decimal CantidadPersonas { get; set; }
    public decimal FactorPonderacion { get; set; }
    public decimal FactorInflacion { get; set; }
    public decimal CostoTotalSemestre { get; set; }

    public CargoFacultad? CargoFacultad { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }
}

public sealed class CargoPlantaCentral : EntidadBase
{
    public string NombreCargo { get; set; } = string.Empty;
    public decimal SueldoMensualTotal { get; set; }

    public ICollection<ProyeccionCargoPlantaCentral> ProyeccionesCargoPlantaCentral { get; set; } = new List<ProyeccionCargoPlantaCentral>();
}

public sealed class ProyeccionCargoPlantaCentral : EntidadBase
{
    public int CargoPlantaCentralId { get; set; }
    public int CarreraId { get; set; }
    public int PeriodoAcademicoId { get; set; }
    public decimal ProporcionAsignacion { get; set; }
    public decimal CostoTotalSemestre { get; set; }

    public CargoPlantaCentral? CargoPlantaCentral { get; set; }
    public Carrera? Carrera { get; set; }
    public PeriodoAcademico? PeriodoAcademico { get; set; }
}
