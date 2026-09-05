using System.Windows;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class CategoryEditDialog : Wpf.Ui.Controls.FluentWindow
{
    public CategoryEditDialog(CategoryEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (s, e) => Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        viewModel.RequestClose += (s, success) =>
        {
            DialogResult = success;
            Close();
        };
    }
}
