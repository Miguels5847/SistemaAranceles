namespace SistemaAranceles.Domain.ValueObjects;

public readonly record struct CorreoInstitucional
{
    public CorreoInstitucional(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !valor.Contains('@'))
        {
            throw new ArgumentException("El correo institucional no es valido.", nameof(valor));
        }

        Valor = valor.Trim().ToLowerInvariant();
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
