using System.Windows;
using System.Windows.Media;
using Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol;

namespace PortableHub.App.Views;

public enum ModernDialogType
{
    Info,
    Warning,
    Danger,
    Question,
    Success
}

public partial class ModernDialog : Window
{
    public bool DialogResultValue { get; private set; }

    public ModernDialog(
        string message,
        string title = "提示",
        ModernDialogType type = ModernDialogType.Info,
        bool isConfirm = false,
        string confirmText = "确定",
        string cancelText = "取消")
    {
        InitializeComponent();

        Title = title;
        AppTitleBar.Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;

        if (!isConfirm)
        {
            CancelButton.Visibility = Visibility.Collapsed;
        }

        ConfigureDialogVisuals(type);
    }

    private void ConfigureDialogVisuals(ModernDialogType type)
    {
        switch (type)
        {
            case ModernDialogType.Danger:
                DialogSymbolIcon.Symbol = Symbol.Delete;
                DialogSymbolIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                IconContainer.Background = new SolidColorBrush(Color.FromArgb(0x18, 0xEF, 0x44, 0x44));
                if (TryFindResource("DangerButton") is Style dangerStyle)
                {
                    ConfirmButton.Style = dangerStyle;
                }
                break;

            case ModernDialogType.Warning:
                DialogSymbolIcon.Symbol = Symbol.Important;
                DialogSymbolIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                IconContainer.Background = new SolidColorBrush(Color.FromArgb(0x18, 0xF5, 0x9E, 0x0B));
                break;

            case ModernDialogType.Question:
                DialogSymbolIcon.Symbol = Symbol.Help;
                DialogSymbolIcon.Foreground = (Brush)FindResource("SystemAccentColorPrimaryBrush");
                IconContainer.Background = new SolidColorBrush(Color.FromArgb(0x18, 0x3B, 0x82, 0xF6));
                break;

            case ModernDialogType.Success:
                DialogSymbolIcon.Symbol = Symbol.Accept;
                DialogSymbolIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                IconContainer.Background = new SolidColorBrush(Color.FromArgb(0x18, 0x10, 0xB9, 0x81));
                break;

            case ModernDialogType.Info:
            default:
                DialogSymbolIcon.Symbol = Symbol.Message;
                DialogSymbolIcon.Foreground = (Brush)FindResource("SystemAccentColorPrimaryBrush");
                IconContainer.Background = new SolidColorBrush(Color.FromArgb(0x18, 0x3B, 0x82, 0xF6));
                break;
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResultValue = false;
        Close();
    }

    public static bool ShowConfirm(
        string message,
        string title = "确认操作",
        bool isDestructive = false,
        Window? owner = null,
        string confirmText = "",
        string cancelText = "取消")
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            return Application.Current.Dispatcher.Invoke(() =>
                ShowConfirm(message, title, isDestructive, owner, confirmText, cancelText));
        }

        string defaultConfirmText = isDestructive ? "删除" : "确定";
        string finalConfirmText = string.IsNullOrWhiteSpace(confirmText) ? defaultConfirmText : confirmText;
        var dialogType = isDestructive ? ModernDialogType.Danger : ModernDialogType.Question;

        var dlg = new ModernDialog(message, title, dialogType, isConfirm: true, finalConfirmText, cancelText);
        SetOwner(dlg, owner);

        dlg.ShowDialog();
        return dlg.DialogResultValue;
    }

    public static void ShowAlert(string message, string title = "提示", Window? owner = null)
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ShowAlert(message, title, owner));
            return;
        }

        var dlg = new ModernDialog(message, title, ModernDialogType.Info, isConfirm: false);
        SetOwner(dlg, owner);
        dlg.ShowDialog();
    }

    public static void ShowWarning(string message, string title = "警告", Window? owner = null)
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ShowWarning(message, title, owner));
            return;
        }

        var dlg = new ModernDialog(message, title, ModernDialogType.Warning, isConfirm: false);
        SetOwner(dlg, owner);
        dlg.ShowDialog();
    }

    public static void ShowError(string message, string title = "错误", Window? owner = null)
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => ShowError(message, title, owner));
            return;
        }

        var dlg = new ModernDialog(message, title, ModernDialogType.Danger, isConfirm: false);
        SetOwner(dlg, owner);
        dlg.ShowDialog();
    }

    private static void SetOwner(Window dlg, Window? explicitOwner)
    {
        try
        {
            var targetOwner = explicitOwner
                ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible && w != dlg)
                ?? Application.Current?.MainWindow;

            if (targetOwner != null && targetOwner.IsLoaded && targetOwner != dlg)
            {
                dlg.Owner = targetOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        catch
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
