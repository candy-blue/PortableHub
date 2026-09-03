using System.Windows;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class ScannerDialog : Window
{
    public ScannerDialog(ScannerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => Close();
    }
}
