using System.Windows;
using System.Windows.Input;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class QuickLauncherWindow : Window
{
    private readonly QuickLauncherViewModel _viewModel;

    public QuickLauncherWindow(QuickLauncherViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.RequestClose += (s, e) => Hide();
    }

    public void Summon()
    {
        _viewModel.OnOpened();
        Show();
        Activate();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            _viewModel.MoveSelection(-1);
            ScrollToSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            _viewModel.MoveSelection(1);
            ScrollToSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _ = _viewModel.LaunchSelectedAsync();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            int index = e.Key - Key.D1;
            _ = _viewModel.LaunchByIndexAsync(index);
            e.Handled = true;
        }
    }

    private void ScrollToSelected()
    {
        if (ResultsListBox.SelectedItem != null)
        {
            ResultsListBox.ScrollIntoView(ResultsListBox.SelectedItem);
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Hide();
    }
}
