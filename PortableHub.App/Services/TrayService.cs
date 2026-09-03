using System.Drawing;
using System.Windows.Forms;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.Services;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripSeparator _recentSeparator;
    private Action? _onShowWindow;
    private Action? _onShowQuickLauncher;
    private Action? _onOpenSettings;
    private Action? _onExit;
    private Func<Task<IReadOnlyList<Software>>>? _getRecentSoftware;
    private Action<Software>? _onLaunchSoftware;

    public TrayService()
    {
        _contextMenu = new ContextMenuStrip();
        _recentSeparator = new ToolStripSeparator();

        _notifyIcon = new NotifyIcon
        {
            Text = "Portable Hub - 便携软件管理器",
            ContextMenuStrip = _contextMenu,
            Visible = false
        };

        // Create a default icon for tray
        _notifyIcon.Icon = CreateDefaultTrayIcon();
        _notifyIcon.DoubleClick += (s, e) => _onShowWindow?.Invoke();

        BuildContextMenu();
    }

    public void Initialize(
        Action onShowWindow,
        Action onShowQuickLauncher,
        Action onOpenSettings,
        Action onExit,
        Func<Task<IReadOnlyList<Software>>> getRecentSoftware,
        Action<Software> onLaunchSoftware)
    {
        _onShowWindow = onShowWindow;
        _onShowQuickLauncher = onShowQuickLauncher;
        _onOpenSettings = onOpenSettings;
        _onExit = onExit;
        _getRecentSoftware = getRecentSoftware;
        _onLaunchSoftware = onLaunchSoftware;

        _notifyIcon.Visible = true;
    }

    private void BuildContextMenu()
    {
        _contextMenu.Items.Clear();

        var openItem = new ToolStripMenuItem("打开主窗口", null, (s, e) => _onShowWindow?.Invoke());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        _contextMenu.Items.Add(openItem);

        var quickLaunchItem = new ToolStripMenuItem("快速启动 (Ctrl+Alt+Space)", null, (s, e) => _onShowQuickLauncher?.Invoke());
        _contextMenu.Items.Add(quickLaunchItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Recent section header
        var recentHeader = new ToolStripMenuItem("最近使用") { Enabled = false };
        _contextMenu.Items.Add(recentHeader);

        _contextMenu.Items.Add(_recentSeparator);

        var settingsItem = new ToolStripMenuItem("设置", null, (s, e) => _onOpenSettings?.Invoke());
        _contextMenu.Items.Add(settingsItem);

        var updateItem = new ToolStripMenuItem("检查更新", null, (s, e) =>
        {
            MessageBox.Show("当前已是最新版本 (v1.0.0)。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Information);
        });
        _contextMenu.Items.Add(updateItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出", null, (s, e) => _onExit?.Invoke());
        _contextMenu.Items.Add(exitItem);

        _contextMenu.Opening += async (s, e) => await RefreshRecentItemsAsync();
    }

    private async Task RefreshRecentItemsAsync()
    {
        if (_getRecentSoftware == null || _onLaunchSoftware == null)
            return;

        try
        {
            var recentList = await _getRecentSoftware();
            var headerIndex = _contextMenu.Items.IndexOf(_recentSeparator);

            // Remove previous dynamic items between recent header and separator
            while (headerIndex > 0 && _contextMenu.Items[headerIndex - 1] is ToolStripMenuItem item && item.Tag is Software)
            {
                _contextMenu.Items.RemoveAt(headerIndex - 1);
                headerIndex--;
            }

            var top8 = recentList.Take(8).ToList();
            if (top8.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(无最近记录)") { Enabled = false };
                _contextMenu.Items.Insert(headerIndex, emptyItem);
            }
            else
            {
                foreach (var sw in top8)
                {
                    var item = new ToolStripMenuItem(sw.Name, null, (s, e) => _onLaunchSoftware(sw))
                    {
                        Tag = sw
                    };
                    _contextMenu.Items.Insert(headerIndex, item);
                    headerIndex++;
                }
            }
        }
        catch
        {
            // Ignore UI update error on menu opening
        }
    }

    private static Icon CreateDefaultTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Draw a clean modern cube / hub icon
        using var brush = new SolidBrush(System.Drawing.Color.FromArgb(59, 130, 246));
        g.FillEllipse(brush, 1, 1, 14, 14);

        using var pen = new Pen(System.Drawing.Color.White, 2);
        g.DrawLine(pen, 5, 8, 11, 8);
        g.DrawLine(pen, 8, 5, 8, 11);

        return Icon.FromHandle(bmp.GetHicon());
    }

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        GC.SuppressFinalize(this);
    }
}
