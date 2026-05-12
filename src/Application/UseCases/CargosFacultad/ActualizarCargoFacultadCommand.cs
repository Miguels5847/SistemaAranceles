using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class ActualizarCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(ActualizarCargoFacultadDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var cargo = await repositorioCargoFacultad.ObtenerPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el cargo con Id {dto.Id}.");

        // RN-96/97: Validar TipoContrato + TarifaHora + SueldoBaseMensual
        ValidarContratoPorTipo(dto.TipoContrato, dto.TarifaHora, dto.SueldoBaseMensual);

        cargo.CambiarCarrera(dto.CarreraId);
        cargo.CambiarNombreCargo(dto.NombreCargo);
        cargo.CambiarTipoCargo(dto.TipoCargo);
        cargo.CambiarSueldoBase(dto.SueldoBaseMensual);
        cargo.CambiarEsCargoDocente(dto.EsCargoDocente);
        cargo.CambiarCantidadDefault(dto.CantidadDefault);
        cargo.CambiarTipoContrato(dto.TipoContrato);
        cargo.CambiarTarifaHora(dto.TarifaHora);

        repositorioCargoFacultad.Actualizar(cargo);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }

    private static void ValidarContratoPorTipo(TipoContrato tipo, decimal tarifaHora, decimal sueldoBaseMensual)
    {
        if (tipo == TipoContrato.TiempoParcial)
        {
            if (tarifaHora <= 0m)
                throw new ArgumentException("Cargo Tiempo Parcial requiere TarifaHora > 0.");
            if (sueldoBaseMensual != 0m)
                throw new ArgumentException("Cargo Tiempo Parcial debe tener SueldoBaseMensual = 0.");
        }
        else
        {
            if (sueldoBaseMensual <= 0m)
                throw new ArgumentException($"Cargo {tipo} requiere SueldoBaseMensual > 0.");
            if (tarifaHora != 0m)
                throw new ArgumentException($"Cargo {tipo} no puede tener TarifaHora (debe ser 0).");
        }
    }
}