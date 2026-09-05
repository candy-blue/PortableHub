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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private bool _isSummoning;

    public void Summon()
    {
        if (IsVisible)
        {
            Hide();
            return;
        }

        _isSummoning = true;
        _viewModel.OnOpened();
        PositionOnCurrentScreen();

        Show();
        Topmost = true;
        Activate();

        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        helper.EnsureHandle();
        SetForegroundWindow(helper.Handle);
        BringWindowToTop(helper.Handle);

        SearchBox.Focus();
        SearchBox.SelectAll();

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
        {
            _isSummoning = false;
        });
    }

    private void PositionOnCurrentScreen()
    {
        try
        {
            var cursorPos = System.Windows.Forms.Cursor.Position;
            var screen = System.Windows.Forms.Screen.FromPoint(cursorPos);
            var workingArea = screen.WorkingArea;

            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            if (dpiX <= 0) dpiX = 1.0;
            if (dpiY <= 0) dpiY = 1.0;

            double screenLeftDip = workingArea.Left / dpiX;
            double screenTopDip = workingArea.Top / dpiY;
            double screenWidthDip = workingArea.Width / dpiX;
            double screenHeightDip = workingArea.Height / dpiY;

            Left = screenLeftDip + (screenWidthDip - Width) / 2.0;
            Top = screenTopDip + (screenHeightDip - Height) / 2.0;
        }
        catch
        {
            // Fallback to center screen if any calculation issue
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2.0;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2.0;
        }
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
        if (!_isSummoning && IsVisible)
        {
            Hide();
        }
    }
}
