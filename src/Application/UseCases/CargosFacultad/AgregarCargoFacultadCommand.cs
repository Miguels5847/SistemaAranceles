using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class AgregarCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(CrearCargoFacultadDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // RN-96/97: Validar TipoContrato + TarifaHora + SueldoBaseMensual
        ValidarContratoPorTipo(dto.TipoContrato, dto.TarifaHora, dto.SueldoBaseMensual);

        var cargo = new CargoFacultad(
            dto.CarreraId,
            dto.NombreCargo,
            dto.TipoCargo,
            dto.SueldoBaseMensual,
            dto.EsCargoDocente,
            dto.CantidadDefault,
            dto.TipoContrato,
            dto.TarifaHora);

        await repositorioCargoFacultad.AgregarAsync(cargo, cancellationToken);
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