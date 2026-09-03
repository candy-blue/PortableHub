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
        Loaded += (s, e) => ThemeService.ApplyDwmAttributes(this, ThemeService.IsWindowsInDarkMode());
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
