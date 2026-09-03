using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;
using PortableHub.Core.Interfaces;

namespace PortableHub.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private Point _dragStartPoint;
    private bool _isRealExit;
    private System.Windows.Threading.DispatcherTimer? _statusRefreshTimer;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = settingsService;
        DataContext = viewModel;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Hook Win32 window message for single instance activation
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);

        // Restore window dimensions and position
        RestoreWindowBounds();

        await _viewModel.InitializeAsync();

        // Setup low-overhead status refresh timer (every 5 seconds, only when active)
        _statusRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _statusRefreshTimer.Tick += (s, ev) =>
        {
            if (IsVisible && WindowState != WindowState.Minimized)
            {
                _viewModel.RefreshRunningStates();
            }
        };
        _statusRefreshTimer.Start();

        // Apply Windows 11 DWM dark titlebar & rounded corners
        var isDark = _settingsService.CurrentSettings.Theme == "Dark" ||
                     (_settingsService.CurrentSettings.Theme == "System" && ThemeService.IsWindowsInDarkMode());
        ThemeService.ApplyDwmAttributes(this, isDark);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == SingleInstanceManager.WM_SHOWME)
        {
            ShowAndActivate();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Show();
        Activate();
        Focus();
    }

    public void ForceExit()
    {
        _isRealExit = true;
        Close();
    }

    private void RestoreWindowBounds()
    {
        var s = _settingsService.CurrentSettings;
        if (s.WindowWidth >= MinWidth) Width = s.WindowWidth;
        if (s.WindowHeight >= MinHeight) Height = s.WindowHeight;

        if (s.WindowLeft.HasValue && s.WindowTop.HasValue)
        {
            // Verify if coordinate is within virtual screen
            if (s.WindowLeft.Value >= SystemParameters.VirtualScreenLeft &&
                s.WindowLeft.Value + Width <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
                s.WindowTop.Value >= SystemParameters.VirtualScreenTop &&
                s.WindowTop.Value + Height <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                Left = s.WindowLeft.Value;
                Top = s.WindowTop.Value;
            }
        }

        if (s.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveWindowBounds()
    {
        var s = _settingsService.CurrentSettings;
        if (WindowState == WindowState.Maximized)
        {
            s.IsMaximized = true;
        }
        else
        {
            s.IsMaximized = false;
            s.WindowWidth = Width;
            s.WindowHeight = Height;
            s.WindowLeft = Left;
            s.WindowTop = Top;
        }
        _ = _settingsService.SaveSettingsAsync();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        SaveWindowBounds();

        if (!_isRealExit && _settingsService.CurrentSettings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.F)
            {
                TopSearchBox.Focus();
                TopSearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.N)
            {
                _ = _viewModel.AddSoftwareAsync();
                e.Handled = true;
            }
            else if (e.Key == Key.OemComma)
            {
                _ = _viewModel.OpenSettingsAsync();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(TopSearchBox.Text))
            {
                TopSearchBox.Text = string.Empty;
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Enter)
        {
            if (_viewModel.SelectedSoftware != null && !TopSearchBox.IsFocused)
            {
                _ = _viewModel.SelectedSoftware.LaunchAsync();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete)
        {
            if (_viewModel.SelectedSoftware != null && !TopSearchBox.IsFocused)
            {
                _viewModel.SelectedSoftware.Delete();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F5)
        {
            _ = _viewModel.RefreshAsync();
            e.Handled = true;
        }
    }

    #region Drag & Drop from Windows Explorer
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        await _viewModel.AddSoftwareFromPathAsync(file);
                    }
                }
            }
        }
    }
    #endregion

    #region Card Drag, Drop and Click Actions
    private void Card_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is SoftwareCardViewModel card)
        {
            _viewModel.SelectedSoftware = card;
        }
    }

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is SoftwareCardViewModel card)
        {
            _viewModel.SelectedSoftware = card;

            // Double click to launch immediately
            if (e.ClickCount >= 2 && e.ChangedButton == MouseButton.Left)
            {
                if (card.LaunchCommand.CanExecute(null))
                {
                    card.LaunchCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }
        }
        _dragStartPoint = e.GetPosition(null);
    }

    private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.Tag is SoftwareCardViewModel card)
        {
            var currentPos = e.GetPosition(null);
            var diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(element, card, DragDropEffects.Move);
            }
        }
    }

    private void Card_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(SoftwareCardViewModel)) is SoftwareCardViewModel sourceCard &&
            sender is FrameworkElement targetElement &&
            targetElement.Tag is SoftwareCardViewModel targetCard &&
            sourceCard.Id != targetCard.Id)
        {
            int sourceIndex = _viewModel.FilteredSoftware.IndexOf(sourceCard);
            int targetIndex = _viewModel.FilteredSoftware.IndexOf(targetCard);
            if (sourceIndex >= 0 && targetIndex >= 0)
            {
                _viewModel.ReorderCards(sourceIndex, targetIndex);
            }
        }
    }
    #endregion

    #region Category Drag and Drop
    private void Category_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(SoftwareCardViewModel)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private async void Category_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(SoftwareCardViewModel)) is SoftwareCardViewModel sourceCard)
        {
            var element = e.OriginalSource as FrameworkElement;
            var catItem = element?.DataContext as CategoryNavModel;
            if (catItem != null && catItem.Id.HasValue && catItem.NavMode == "Category")
            {
                await _viewModel.MoveSoftwareToCategoryAsync(sourceCard, catItem.Id.Value);
            }
        }
    }
    #endregion
}
