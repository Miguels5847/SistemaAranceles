using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace SistemaAranceles.Presentation.Behaviors;

/// <summary>
/// Auto-descarta un mensaje de éxito ~5 s después de mostrarse (KAN-49): los mensajes quedaban
/// pegados y no se sabía si eran de la acción actual o de una anterior. En vez de ocultar solo el
/// TextBlock (lo que dejaba un Border de color vacío cuando el contenedor también ata su Visibility
/// al texto), <b>limpia la propiedad de origen</b> (p. ej. MensajeExito) a través del binding: así
/// el texto y su Border se ocultan juntos en todas las vistas. Los mensajes de ERROR no usan este
/// behavior: deben quedarse visibles.
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
                Limpiar(texto);
            };
            texto.SetValue(TimerProperty, timer);
        }

        timer.Stop();
        if (!string.IsNullOrWhiteSpace(texto.Text))
            timer.Start();
    }

    /// <summary>
    /// Pone en blanco la propiedad de origen del binding de Text (ej. MensajeExito). Como el Border
    /// de color y el TextBlock atan su Visibility a esa misma cadena, ambos desaparecen. Si no se
    /// puede resolver la fuente, al menos colapsa el TextBlock (comportamiento anterior).
    /// </summary>
    private static void Limpiar(TextBlock texto)
    {
        var expresion = texto.GetBindingExpression(TextBlock.TextProperty);
        if (expresion?.ResolvedSource is { } fuente && !string.IsNullOrEmpty(expresion.ResolvedSourcePropertyName))
        {
            var propiedad = fuente.GetType().GetProperty(expresion.ResolvedSourcePropertyName);
            if (propiedad is { CanWrite: true } && propiedad.PropertyType == typeof(string))
            {
                propiedad.SetValue(fuente, string.Empty);
                return;
            }
        }

        texto.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
    }
}
