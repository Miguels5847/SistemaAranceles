using System.Text;
using System.Windows;

namespace SistemaAranceles.Presentation.Views.Compartido;

public partial class AyudaModulo
{
    public AyudaModulo()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty TituloProperty = DependencyProperty.Register(
        nameof(Titulo), typeof(string), typeof(AyudaModulo), new PropertyMetadata(string.Empty, Recomponer));

    public static readonly DependencyProperty IngresarProperty = DependencyProperty.Register(
        nameof(Ingresar), typeof(string), typeof(AyudaModulo), new PropertyMetadata(string.Empty, Recomponer));

    public static readonly DependencyProperty ResultadosProperty = DependencyProperty.Register(
        nameof(Resultados), typeof(string), typeof(AyudaModulo), new PropertyMetadata(string.Empty, Recomponer));

    public string Titulo { get => (string)GetValue(TituloProperty); set => SetValue(TituloProperty, value); }
    public string Ingresar { get => (string)GetValue(IngresarProperty); set => SetValue(IngresarProperty, value); }
    public string Resultados { get => (string)GetValue(ResultadosProperty); set => SetValue(ResultadosProperty, value); }

    private static void Recomponer(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (AyudaModulo)d;
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(c.Titulo))
            sb.Append(c.Titulo).Append("\n\n");
        if (!string.IsNullOrWhiteSpace(c.Ingresar))
            sb.Append("▸ QUÉ INGRESAR AQUÍ:\n").Append(c.Ingresar);
        if (!string.IsNullOrWhiteSpace(c.Resultados))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append("▸ QUÉ RESULTADOS VERÁS:\n").Append(c.Resultados);
        }
        c.Badge.ToolTip = sb.ToString();
    }
}
