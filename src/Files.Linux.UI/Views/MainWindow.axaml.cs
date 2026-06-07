using Avalonia.Controls;
using Files.Linux.UI.ViewModels;

namespace Files.Linux.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}