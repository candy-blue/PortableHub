using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
            var isDark = viewModel.Theme == "Dark" ||
                         (viewModel.Theme == "System" && ThemeService.IsWindowsInDarkMode());
            ThemeService.ApplyDwmAttributes(this, isDark);
        };
        viewModel.RequestClose += (s, e) => Close();
    }
}
