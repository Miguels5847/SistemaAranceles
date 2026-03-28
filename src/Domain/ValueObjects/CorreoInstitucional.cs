using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.ValueObjects;

public readonly record struct CorreoInstitucional
{
    public CorreoInstitucional(string valor)
    {
        Valor = GuardiaDominio.CorreoValido(valor, 150);
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
