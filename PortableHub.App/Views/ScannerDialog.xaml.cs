using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class ScannerDialog : Wpf.Ui.Controls.FluentWindow
{
    public ScannerDialog(ScannerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        viewModel.RequestClose += (s, e) => Close();
    }
}
