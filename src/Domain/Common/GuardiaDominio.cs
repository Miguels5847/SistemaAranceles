using System.Net.Mail;

namespace SistemaAranceles.Domain.Common;

public static class GuardiaDominio
{
    public static string Requerido(string? valor, string nombreCampo, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DominioException($"{nombreCampo} es obligatorio.");
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > longitudMaxima)
        {
            throw new DominioException($"{nombreCampo} supera la longitud maxima permitida de {longitudMaxima}.");
        }

        return normalizado;
    }

    public static int EnteroPositivo(int valor, string nombreCampo)
    {
        if (valor <= 0)
        {
            throw new DominioException($"{nombreCampo} debe ser mayor a cero.");
        }

        return valor;
    }

    public static int EnteroNoNegativo(int valor, string nombreCampo)
    {
        if (valor < 0)
        {
            throw new DominioException($"{nombreCampo} no puede ser negativo.");
        }

        return valor;
    }

    public static decimal DecimalNoNegativo(decimal valor, string nombreCampo, int decimales)
    {
        if (valor < 0)
        {
            throw new DominioException($"{nombreCampo} no puede ser negativo.");
        }

        return decimal.Round(valor, decimales);
    }

    public static decimal Porcentaje(decimal valor, string nombreCampo)
    {
        if (valor < 0 || valor > 100)
        {
            throw new DominioException($"{nombreCampo} debe estar entre 0 y 100.");
        }

        return decimal.Round(valor, 4);
    }

    public static string CorreoValido(string valor, int longitudMaxima)
    {
        var correo = Requerido(valor, "Correo institucional", longitudMaxima).ToLowerInvariant();

        try
        {
            _ = new MailAddress(correo);
        }
        catch
        {
            throw new DominioException("Correo institucional no tiene un formato valido.");
        }

        return correo;
    }
}
