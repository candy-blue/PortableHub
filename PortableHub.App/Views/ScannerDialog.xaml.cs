using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class ScannerDialog : Window
{
    public ScannerDialog(ScannerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => ThemeService.ApplyDwmAttributes(this, ThemeService.IsWindowsInDarkMode());
        viewModel.RequestClose += (s, e) => Close();
    }
}
