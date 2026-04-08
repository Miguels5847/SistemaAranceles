using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SistemaAranceles.Presentation.ViewModels.Usuarios;

namespace SistemaAranceles.Presentation.Views.Usuarios;

public partial class UsuariosView : UserControl
{
    public UsuariosView()
    {
        InitializeComponent();
        this.Loaded += (s, e) => IniciarAnimacionCarga();
    }

    private void IniciarAnimacionCarga()
    {
        var rotation = new RotateTransform { CenterX = 24, CenterY = 24 };
        var animation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = RepeatBehavior.Forever
        };
        rotation.BeginAnimation(RotateTransform.AngleProperty, animation);
        LoadingSymbol.RenderTransform = rotation;
    }
}
