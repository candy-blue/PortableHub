using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SoftwareEditDialog : Wpf.Ui.Controls.FluentWindow
{
    public SoftwareEditDialog(SoftwareEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        };
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
