using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class CategoryEditDialog : Window
{
    public CategoryEditDialog(CategoryEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }

    private void IconButton_Click(object sender, RoutedEventArgs e)
    {
        IconFlyout.Hide();
    }
}
