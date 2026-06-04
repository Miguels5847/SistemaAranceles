namespace SistemaAranceles.Domain.Enums;

/// <summary>
/// Modo de cálculo financiero para VAN/TIR/Flujo de Fondos.
/// CompatibleExcel: replica la matriz del tutor — flujos SEMESTRALES, período 0 descontado en el VAN
/// (convención NPV de Excel) y SIN recuperación de capital de trabajo. Modo principal para validar.
/// Tecnico: enfoque financiero ortodoxo — flujo ANUAL consolidado, período 0 sin descontar y CON
/// recuperación de capital de trabajo.
/// </summary>
public enum ModoCalculoFinanciero
{
    CompatibleExcel = 0,
    Tecnico = 1
}
