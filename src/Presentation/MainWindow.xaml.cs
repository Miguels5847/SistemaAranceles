using System.Windows;
using SistemaAranceles.Presentation.ViewModels;

namespace SistemaAranceles.Presentation;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
