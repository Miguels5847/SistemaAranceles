using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace SistemaAranceles.Infrastructure.Export;

internal static class EstilosPdf
{
    public const string ColorPrimario = "#1A237E";
    public const string ColorGris = "#546E7A";
    public const string ColorFondoHeader = "#E8EAF6";
    public const string ColorFondoSeccion = "#EEF2FF";
    public const string ColorBorde = "#C5CAE9";

    public static IContainer CeldaHeader(this IContainer c) => c
        .Background(ColorFondoHeader)
        .Border(0.5f).BorderColor(ColorBorde)
        .PaddingVertical(4).PaddingHorizontal(5);

    public static IContainer Celda(this IContainer c) => c
        .Border(0.5f).BorderColor(ColorBorde)
        .PaddingVertical(3).PaddingHorizontal(5);

    public static IContainer CeldaSeccion(this IContainer c) => c
        .Background(ColorFondoSeccion)
        .Border(0.5f).BorderColor(ColorBorde)
        .PaddingVertical(3).PaddingHorizontal(5);

    public static void EncabezadoReporte(IContainer header, string titulo, string carrera, string escenario, DateTime generadoEn)
    {
        header.Column(col =>
        {
            col.Item().Text(titulo).FontSize(15).Bold().FontColor(ColorPrimario);
            col.Item().Text($"Carrera: {carrera}    |    Escenario: {escenario}")
                .FontSize(9).FontColor(ColorGris);
            col.Item().Text($"Generado: {generadoEn:dd/MM/yyyy HH:mm}  —  Sistema de Aranceles Universitarios")
                .FontSize(8).FontColor(ColorGris);
            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(ColorPrimario);
            col.Item().PaddingBottom(6);
        });
    }

    public static void PiePagina(IContainer footer)
    {
        footer.AlignRight().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor(ColorGris));
            t.Span("Página ");
            t.CurrentPageNumber();
            t.Span(" de ");
            t.TotalPages();
        });
    }

    public static void TituloSeccion(ColumnDescriptor col, string texto)
    {
        col.Item().PaddingTop(10).Text(texto).FontSize(11).Bold().FontColor(ColorPrimario);
        col.Item().PaddingBottom(4);
    }

    public static void Advertencia(ColumnDescriptor col, string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return;
        col.Item().PaddingTop(6).Text($"Advertencia: {mensaje}")
            .FontSize(8).Italic().FontColor("#C62828");
    }
}
