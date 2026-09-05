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

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private Point _dragStartPoint;
    private bool _isRealExit;
    private System.Windows.Threading.DispatcherTimer? _statusRefreshTimer;
    private System.Windows.Threading.DispatcherTimer? _dragWatchdogTimer;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsService = settingsService;
        DataContext = viewModel;

        Loaded += MainWindow_Loaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Restore window dimensions and position
        RestoreWindowBounds();

        await _viewModel.InitializeAsync();

        // Listen for sidebar collapse/expand animation
        _viewModel.PropertyChanged += (s, ev) =>
        {
            if (ev.PropertyName == nameof(MainViewModel.IsSidebarCollapsed))
            {
                AnimateSidebar(_viewModel.IsSidebarCollapsed);
            }
        };

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

        // Watch system theme changes for native Mica backdrop
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
    }

    private void AnimateSidebar(bool collapsed)
    {
        double targetWidth = collapsed ? 56.0 : 220.0;
        var anim = new System.Windows.Media.Animation.DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(160))
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
        };
        SidebarBorder.BeginAnimation(WidthProperty, anim);
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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Show();
        Activate();
        Focus();

        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();
        SetForegroundWindow(helper.Handle);
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _settingsService.CurrentSettings.MinimizeToTray)
        {
            Hide();
        }
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
        else
        {
            System.Windows.Application.Current.Shutdown();
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
            else if (e.Key == Key.B)
            {
                _viewModel.ToggleSidebarCommand.Execute(null);
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
            var target = _viewModel.SelectedSoftware ?? _viewModel.FilteredSoftware.FirstOrDefault();
            if (target != null && !TopSearchBox.IsFocused)
            {
                _ = target.LaunchAsync();
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

    private void TopSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (_viewModel.FilteredSoftware.Count > 0)
            {
                int currentIndex = _viewModel.SelectedSoftware != null
                    ? _viewModel.FilteredSoftware.IndexOf(_viewModel.SelectedSoftware)
                    : -1;
                int nextIndex = Math.Min(_viewModel.FilteredSoftware.Count - 1, currentIndex + 1);
                _viewModel.SelectedSoftware = _viewModel.FilteredSoftware[nextIndex];
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            if (_viewModel.FilteredSoftware.Count > 0)
            {
                int currentIndex = _viewModel.SelectedSoftware != null
                    ? _viewModel.FilteredSoftware.IndexOf(_viewModel.SelectedSoftware)
                    : -1;
                int prevIndex = Math.Max(0, currentIndex - 1);
                _viewModel.SelectedSoftware = _viewModel.FilteredSoftware[prevIndex];
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Enter)
        {
            var target = _viewModel.SelectedSoftware ?? _viewModel.FilteredSoftware.FirstOrDefault();
            if (target != null)
            {
                _ = target.LaunchAsync();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            TopSearchBox.Text = string.Empty;
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    #region Drag & Drop from Windows Explorer & Overlay
    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropOverlay.Visibility = Visibility.Visible;
            e.Handled = true;

            // Reset watchdog timer (auto-clears overlay if drag is abandoned or dropped outside)
            if (_dragWatchdogTimer == null)
            {
                _dragWatchdogTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(250)
                };
                _dragWatchdogTimer.Tick += (s, ev) =>
                {
                    bool isMouseButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0 || (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
                    bool isCursorInside = false;
                    if (GetCursorPos(out var pt))
                    {
                        try
                        {
                            var screenTopLeft = PointToScreen(new Point(0, 0));
                            var screenBottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
                            isCursorInside = new Rect(screenTopLeft, screenBottomRight).Contains(new Point(pt.X, pt.Y));
                        }
                        catch { }
                    }

                    if (!isMouseButtonDown || !isCursorInside)
                    {
                        DropOverlay.Visibility = Visibility.Collapsed;
                        _dragWatchdogTimer.Stop();
                    }
                };
            }
            _dragWatchdogTimer.Stop();
            _dragWatchdogTimer.Start();
        }
        else if (e.Data.GetDataPresent(typeof(SoftwareCardViewModel)) || e.Data.GetDataPresent(typeof(CategoryNavModel)))
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _dragWatchdogTimer?.Stop();
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _dragWatchdogTimer?.Stop();
            e.Effects = DragDropEffects.None;
        }
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        // When drag leaves towards taskbar, desktop, or other windows, verify real screen cursor position
        if (GetCursorPos(out var pt))
        {
            try
            {
                var screenTopLeft = PointToScreen(new Point(0, 0));
                var screenBottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
                var screenRect = new Rect(screenTopLeft, screenBottomRight);

                if (!screenRect.Contains(new Point(pt.X, pt.Y)))
                {
                    DropOverlay.Visibility = Visibility.Collapsed;
                    _dragWatchdogTimer?.Stop();
                    return;
                }
            }
            catch
            {
                DropOverlay.Visibility = Visibility.Collapsed;
                _dragWatchdogTimer?.Stop();
                return;
            }
        }

        if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) == 0 && (GetAsyncKeyState(VK_RBUTTON) & 0x8000) == 0)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _dragWatchdogTimer?.Stop();
            return;
        }

        var pos = e.GetPosition(this);
        if (pos.X <= 0 || pos.Y <= 0 || pos.X >= ActualWidth || pos.Y >= ActualHeight)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _dragWatchdogTimer?.Stop();
        }
    }

    private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        // If user moves mouse normally without dragging, ensure stuck overlay is dismissed
        if (DropOverlay.Visibility == Visibility.Visible && e.LeftButton == MouseButtonState.Released)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            _dragWatchdogTimer?.Stop();
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        DropOverlay.Visibility = Visibility.Collapsed;
        _dragWatchdogTimer?.Stop();
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        _dragWatchdogTimer?.Stop();

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    await _viewModel.AddSoftwareFromPathAsync(file);
                }
            }
            e.Handled = true;
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

    private void CardMoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement btn)
        {
            var contextMenu = FindResource("SoftwareItemContextMenu") as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.PlacementTarget = btn;
                contextMenu.DataContext = btn.DataContext;
                contextMenu.IsOpen = true;
                e.Handled = true;
            }
        }
    }

    private void SoftwareItemContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu)
        {
            var card = (menu.DataContext as SoftwareCardViewModel)
                       ?? ((menu.PlacementTarget as FrameworkElement)?.DataContext as SoftwareCardViewModel);
            var moveItem = menu.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == "移动到分类");
            if (moveItem != null)
            {
                moveItem.Items.Clear();
                foreach (var cat in _viewModel.CustomCategories)
                {
                    if (!cat.Id.HasValue) continue;
                    var subItem = new MenuItem
                    {
                        Header = cat.Name,
                        IsChecked = card != null && card.CategoryId == cat.Id.Value
                    };
                    var catId = cat.Id.Value;
                    subItem.Click += async (_, _) =>
                    {
                        if (card != null)
                        {
                            await _viewModel.MoveSoftwareToCategoryAsync(card, catId);
                        }
                    };
                    moveItem.Items.Add(subItem);
                }
                moveItem.IsEnabled = moveItem.Items.Count > 0;
            }
        }
    }

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is SoftwareCardViewModel card)
        {
            _viewModel.SelectedSoftware = card;

            // Double click to launch immediately (when mode is DoubleClick)
            if (_viewModel.LaunchClickMode != "SingleClick" && e.ClickCount >= 2 && e.ChangedButton == MouseButton.Left)
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

    private void Card_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Single click to launch immediately (when mode is SingleClick)
        if (_viewModel.LaunchClickMode == "SingleClick" && sender is FrameworkElement element && element.Tag is SoftwareCardViewModel card)
        {
            // Do not trigger if user clicked an inner action button (e.g. Star, More, Play)
            if (e.OriginalSource is DependencyObject dep && FindVisualParent<System.Windows.Controls.Primitives.ButtonBase>(dep) != null)
            {
                return;
            }

            var currentPos = e.GetPosition(null);
            var diff = _dragStartPoint - currentPos;
            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                if (card.LaunchCommand.CanExecute(null))
                {
                    card.LaunchCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindVisualParent<T>(parentObject);
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

    private async void Card_Drop(object sender, DragEventArgs e)
    {
        // 1. Support dropping files from explorer directly on cards
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    await _viewModel.AddSoftwareFromPathAsync(file);
                }
            }
            e.Handled = true;
            return;
        }

        // 2. Support reordering software cards
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
            e.Handled = true;
        }
    }
    #endregion

    #region Category Drag, Drop and Reorder
    private Point _categoryDragStartPoint;

    private void CategoryItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _categoryDragStartPoint = e.GetPosition(null);
    }

    private void CategoryItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.Tag is CategoryNavModel catItem)
        {
            var currentPos = e.GetPosition(null);
            var diff = _categoryDragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(element, catItem, DragDropEffects.Move);
            }
        }
    }

    private void CategoryItem_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(CategoryNavModel)) || e.Data.GetDataPresent(typeof(SoftwareCardViewModel)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void CategoryItem_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;

        if (sender is not FrameworkElement targetElement || targetElement.Tag is not CategoryNavModel targetCat)
            return;

        // 1. Reorder categories: dragged category over another category
        if (e.Data.GetData(typeof(CategoryNavModel)) is CategoryNavModel sourceCat && sourceCat.Id.HasValue && targetCat.Id.HasValue)
        {
            if (sourceCat.Id != targetCat.Id)
            {
                int sourceIndex = _viewModel.CustomCategories.IndexOf(sourceCat);
                int targetIndex = _viewModel.CustomCategories.IndexOf(targetCat);
                if (sourceIndex >= 0 && targetIndex >= 0)
                {
                    _viewModel.ReorderCategories(sourceIndex, targetIndex);
                }
            }
            e.Handled = true;
            return;
        }

        // 2. Move existing software card into this category
        if (e.Data.GetData(typeof(SoftwareCardViewModel)) is SoftwareCardViewModel sourceCard && targetCat.Id.HasValue)
        {
            await _viewModel.MoveSoftwareToCategoryAsync(sourceCard, targetCat.Id.Value);
            e.Handled = true;
            return;
        }

        // 3. Drop files from Explorer directly into this category
        if (e.Data.GetDataPresent(DataFormats.FileDrop) && targetCat.Id.HasValue)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null)
            {
                foreach (var file in files)
                {
                    await _viewModel.AddSoftwareFromPathAsync(file, targetCat.Id.Value);
                }
            }
            e.Handled = true;
        }
    }

    private void Category_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(SoftwareCardViewModel)) || e.Data.GetDataPresent(typeof(CategoryNavModel)))
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private async void Category_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;

        var element = e.OriginalSource as FrameworkElement;
        var catItem = (element?.DataContext as CategoryNavModel) ?? (element?.Tag as CategoryNavModel);

        if (catItem != null && catItem.Id.HasValue && catItem.NavMode == "Category")
        {
            if (e.Data.GetData(typeof(SoftwareCardViewModel)) is SoftwareCardViewModel sourceCard)
            {
                await _viewModel.MoveSoftwareToCategoryAsync(sourceCard, catItem.Id.Value);
                e.Handled = true;
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        await _viewModel.AddSoftwareFromPathAsync(file, catItem.Id.Value);
                    }
                }
                e.Handled = true;
            }
        }
    }
    #endregion
}
