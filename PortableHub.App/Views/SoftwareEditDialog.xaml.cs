using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SoftwareEditDialog : Window
{
    public SoftwareEditDialog(SoftwareEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
            ThemeService.ApplyDwmAttributes(this, ThemeService.IsWindowsInDarkMode());
        };
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
