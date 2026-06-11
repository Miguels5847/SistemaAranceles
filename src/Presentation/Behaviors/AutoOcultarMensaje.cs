using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SistemaAranceles.Presentation.Behaviors;

/// <summary>
/// Oculta el TextBlock de mensaje de éxito ~5 s después de mostrarse (KAN-49): los mensajes
/// quedaban pegados y no se sabía si eran de la acción actual o de una anterior. Usa
/// SetCurrentValue, así el binding de Visibility original sigue vivo y el siguiente mensaje
/// vuelve a mostrarlo. Los mensajes de ERROR no usan este behavior: deben quedarse visibles.
/// </summary>
public static class AutoOcultarMensaje
{
    private static readonly TimeSpan Retardo = TimeSpan.FromSeconds(5);

    public static readonly DependencyProperty HabilitadoProperty = DependencyProperty.RegisterAttached(
        "Habilitado",
        typeof(bool),
        typeof(AutoOcultarMensaje),
        new PropertyMetadata(false, OnHabilitadoChanged));

    private static readonly DependencyProperty TimerProperty = DependencyProperty.RegisterAttached(
        "Timer",
        typeof(DispatcherTimer),
        typeof(AutoOcultarMensaje));

    public static void SetHabilitado(DependencyObject elemento, bool valor) => elemento.SetValue(HabilitadoProperty, valor);
    public static bool GetHabilitado(DependencyObject elemento) => (bool)elemento.GetValue(HabilitadoProperty);

    private static void OnHabilitadoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock texto || e.NewValue is not true)
            return;

        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        descriptor.AddValueChanged(texto, (_, _) => Reiniciar(texto));
        Reiniciar(texto);
    }

    private static void Reiniciar(TextBlock texto)
    {
        var timer = (DispatcherTimer?)texto.GetValue(TimerProperty);
        if (timer is null)
        {
            timer = new DispatcherTimer { Interval = Retardo };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                texto.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            };
            texto.SetValue(TimerProperty, timer);
        }

        timer.Stop();
        if (string.IsNullOrWhiteSpace(texto.Text))
            return;

        texto.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
        timer.Start();
    }
}
