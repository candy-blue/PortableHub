using System.Windows;
using System.Windows.Input;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;

namespace PortableHub.App.Views;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        };
        viewModel.RequestClose += (s, e) => Close();
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

        if (modifiers.Count > 0)
        {
            e.Handled = true;
            var combo = string.Join("+", modifiers) + "+" + keyName;
            if (DataContext is SettingsViewModel vm)
            {
                vm.GlobalHotkey = combo;
            }
            if (sender is Wpf.Ui.Controls.TextBox tb)
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
            if (sender is Wpf.Ui.Controls.TextBox tb)
            {
                tb.Text = combo;
                tb.CaretIndex = combo.Length;
            }
        }
    }
}
