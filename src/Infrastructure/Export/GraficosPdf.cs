using QuestPDF.Fluent;

namespace SistemaAranceles.Infrastructure.Export;

/// <summary>
/// Gráficos de barras con primitivas QuestPDF (sin dependencias de charting):
/// cada barra es una caja con alto proporcional al valor sobre el máximo de la serie.
/// </summary>
internal static class GraficosPdf
{
    private const float AlturaBarras = 110f;
    // Las barras escalan hasta AlturaBarras-9 para dejar siempre sitio al
    // número del valor justo encima de cada barra (sin desbordar el lienzo).
    private const float AltoMaxBarra = AlturaBarras - 9f;

    public static readonly string[] Paleta =
        ["#1A237E", "#2E7D32", "#F9A825", "#C62828", "#6A1B9A", "#00838F", "#5D4037", "#37474F"];

    public static void GraficoBarras(
        ColumnDescriptor col,
        string titulo,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<(string Nombre, IReadOnlyList<decimal> Valores, string Color)> series)
    {
        EstilosPdf.TituloSeccion(col, titulo);

        var max = series.SelectMany(s => s.Valores).DefaultIfEmpty(0m).Max();
        if (etiquetas.Count == 0 || max <= 0m)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            return;
        }

        if (series.Count > 1)
            ComponerLeyenda(col, series);

        col.Item().Row(row =>
        {
            row.Spacing(6);
            for (var i = 0; i < etiquetas.Count; i++)
                ComponerGrupo(row, etiquetas[i], i, series, max);
        });
    }

    private static void ComponerLeyenda(
        ColumnDescriptor col,
        IReadOnlyList<(string Nombre, IReadOnlyList<decimal> Valores, string Color)> series)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.Spacing(10);
            foreach (var serie in series)
            {
                row.AutoItem().Row(r =>
                {
                    r.AutoItem().PaddingTop(1).Width(7).Height(7).Background(serie.Color);
                    r.AutoItem().PaddingLeft(3).Text(serie.Nombre).FontSize(7);
                });
            }
        });
    }

    private static void ComponerGrupo(
        RowDescriptor row,
        string etiqueta,
        int indice,
        IReadOnlyList<(string Nombre, IReadOnlyList<decimal> Valores, string Color)> series,
        decimal max)
    {
        row.RelativeItem().Column(grupo =>
        {
            grupo.Item().Height(AlturaBarras)
                .BorderBottom(0.8f).BorderColor(EstilosPdf.ColorBorde)
                .Row(barras =>
                {
                    barras.Spacing(1);
                    foreach (var serie in series)
                    {
                        var valor = indice < serie.Valores.Count ? serie.Valores[indice] : 0m;
                        var color = serie.Color;
                        barras.RelativeItem().Column(barra => ComponerBarra(barra, valor, max, color));
                    }
                });
            grupo.Item().PaddingTop(2).AlignCenter().Text(etiqueta).FontSize(6.5f);
        });
    }

    private static void ComponerBarra(ColumnDescriptor barra, decimal valor, decimal max, string color)
    {
        var alto = (float)(valor / max) * AltoMaxBarra;
        if (valor > 0m && alto < 0.8f)
            alto = 0.8f;

        var espacio = Math.Max(AlturaBarras - alto, 0f);
        if (valor > 0m)
            barra.Item().Height(espacio).AlignBottom().AlignCenter()
                .Text(valor.ToString("N0")).FontSize(5.5f).FontColor(EstilosPdf.ColorGris);
        else
            barra.Item().Height(espacio);

        if (alto > 0f)
            barra.Item().Height(alto).Background(color);
    }
}
