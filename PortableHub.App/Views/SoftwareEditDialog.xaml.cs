using System.Windows;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SoftwareEditDialog : Window
{
    public SoftwareEditDialog(SoftwareEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.InitializeAsync();
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
