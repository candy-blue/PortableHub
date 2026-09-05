using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _isClosing;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
        };
        viewModel.RequestClose += (s, e) =>
        {
            SafeClose();
        };
        Closing += (s, e) =>
        {
            _isClosing = true;
            _viewModel.RollbackThemePreview();
        };

        SettingsNavListBox.SelectionChanged += (s, e) =>
        {
            AnimateTabContentEntrance();
        };
    }

    private void AnimateTabContentEntrance()
    {
        if (SettingsTabControl == null) return;

        try
        {
            var duration = TimeSpan.FromMilliseconds(140);
            var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0.4, 1.0, duration) { EasingFunction = ease };
            var slideIn = new System.Windows.Media.Animation.DoubleAnimation(8.0, 0.0, duration) { EasingFunction = ease };

            if (SettingsTabControl.RenderTransform is not System.Windows.Media.TranslateTransform tt || tt.IsFrozen)
            {
                tt = new System.Windows.Media.TranslateTransform(0, 0);
                SettingsTabControl.RenderTransform = tt;
            }

            SettingsTabControl.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            tt.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideIn);
        }
        catch
        {
            // Defensive: ignore animation errors during high-frequency tab switching
        }
    }

    private void SafeClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        try
        {
            Close();
        }
        catch (InvalidOperationException)
        {
            // Ignore if window is already closing or closed
        }
    }

    private void HotkeyInputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        var modifiers = new List<string>();
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers.Add("Ctrl");
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers.Add("Alt");
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers.Add("Shift");
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) modifiers.Add("Win");

        string keyName = key switch
        {
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemBackslash => "\\",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemTilde => "`",
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            _ when key >= Key.A && key <= Key.Z => key.ToString(),
            _ when key >= Key.D0 && key <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
            _ when key >= Key.F1 && key <= Key.F24 => key.ToString(),
            _ => key.ToString()
        };

        if (modifiers.Count > 0 || (key >= Key.F1 && key <= Key.F24))
        {
            e.Handled = true;
            var combo = modifiers.Count > 0 ? string.Join("+", modifiers) + "+" + keyName : keyName;
            if (DataContext is SettingsViewModel vm)
            {
                vm.GlobalHotkey = combo;
            }
            if (sender is TextBox tb)
            {
                tb.Text = combo;
                tb.CaretIndex = combo.Length;
            }
        }
    }

    private void HotkeyInputBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.XButton1 || e.ChangedButton == MouseButton.XButton2)
        {
            e.Handled = true;
            var modifiers = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers.Add("Shift");

            string btnName = e.ChangedButton switch
            {
                MouseButton.Middle => "鼠标中键",
                MouseButton.XButton1 => "鼠标侧键1",
                MouseButton.XButton2 => "鼠标侧键2",
                _ => "鼠标键"
            };

            var combo = modifiers.Count > 0 ? string.Join("+", modifiers) + "+" + btnName : btnName;
            if (DataContext is SettingsViewModel vm)
            {
                vm.GlobalHotkey = combo;
            }
            if (sender is TextBox tb)
            {
                tb.Text = combo;
                tb.CaretIndex = combo.Length;
            }
        }
    }
}
