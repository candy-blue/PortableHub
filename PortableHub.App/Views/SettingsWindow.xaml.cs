using System.Windows;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.InitializeAsync();
        viewModel.RequestClose += (s, e) => Close();
    }
}
