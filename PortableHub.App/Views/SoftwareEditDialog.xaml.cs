using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
        };
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }

    private void OnRunAsAdminRowClicked(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SoftwareEditViewModel vm)
        {
            vm.RunAsAdmin = !vm.RunAsAdmin;
        }
    }

    private void OnSingleInstanceRowClicked(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SoftwareEditViewModel vm)
        {
            vm.SingleInstance = !vm.SingleInstance;
        }
    }

    private void OnRunAsAdminRowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && DataContext is SoftwareEditViewModel vm)
        {
            vm.RunAsAdmin = !vm.RunAsAdmin;
            e.Handled = true;
        }
    }

    private void OnSingleInstanceRowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && DataContext is SoftwareEditViewModel vm)
        {
            vm.SingleInstance = !vm.SingleInstance;
            e.Handled = true;
        }
    }
}
