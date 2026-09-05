using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PortableHub.App.Services;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;

namespace PortableHub.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly IBackupService _backupService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IRootDirectoryRepository _rootRepository;
    private readonly ISoftwareRepository _softwareRepository;
    private readonly IPathRepairService _pathRepairService;
    private readonly ThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string _theme = "System";

    [ObservableProperty]
    private string _language = "zh-CN";

    [ObservableProperty]
    private string _cardSize = "Medium";

    [ObservableProperty]
    private string _viewMode = "Grid";

    [ObservableProperty]
    private string _launchClickMode = "DoubleClick";

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _startMinimizedToTray;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _closeToTray = true;

    [ObservableProperty]
    private string _globalHotkey = "Ctrl+Alt+Space";

    [ObservableProperty]
    private string _backupFrequency = "Weekly";

    [ObservableProperty]
    private int _maxBackupCount = 5;

    private string _originalTheme = "System";
    private string _originalLanguage = "zh-CN";

    [ObservableProperty]
    private string _dataDirectory = string.Empty;

    [ObservableProperty]
    private bool _isPortableMode;

    [ObservableProperty]
    private string _hotkeyStatus = "✓ 快捷键可用";

    [ObservableProperty]
    private bool _isHotkeyValid = true;

    [ObservableProperty]
    private string _currentAppVersion = "v1.0.0";

    [ObservableProperty]
    private bool _isCheckingUpdates;

    [ObservableProperty]
    private string? _updateStatusMessage;

    [ObservableProperty]
    private bool _hasNewUpdate;

    [ObservableProperty]
    private UpdateInfo? _latestUpdateInfo;

    public IReadOnlyList<LanguageOption> SupportedLanguages => _localizationService.SupportedLanguages;

    public ObservableCollection<RootDirectory> RootDirectories { get; } = [];
    public ObservableCollection<string> ExistingBackups { get; } = [];

    public event EventHandler? RequestClose;

    public SettingsViewModel(
        ISettingsService settingsService,
        IStartupService startupService,
        IBackupService backupService,
        IHotkeyService hotkeyService,
        IRootDirectoryRepository rootRepository,
        ISoftwareRepository softwareRepository,
        IPathRepairService pathRepairService,
        ThemeService themeService,
        ILocalizationService? localizationService = null,
        IUpdateService? updateService = null)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _backupService = backupService;
        _hotkeyService = hotkeyService;
        _rootRepository = rootRepository;
        _softwareRepository = softwareRepository;
        _pathRepairService = pathRepairService;
        _themeService = themeService;
        _localizationService = localizationService ?? LocalizationService.Instance;
        _updateService = updateService ?? new GithubUpdateService();
    }

    public async Task InitializeAsync()
    {
        var s = _settingsService.CurrentSettings;
        _originalTheme = s.Theme;
        Theme = s.Theme;
        _originalLanguage = string.IsNullOrWhiteSpace(s.Language) ? "zh-CN" : s.Language;
        Language = _originalLanguage;
        CardSize = s.CardSize;
        ViewMode = s.ViewMode;
        LaunchClickMode = string.IsNullOrEmpty(s.LaunchClickMode) ? "DoubleClick" : s.LaunchClickMode;
        StartWithWindows = _startupService.IsAutoStartEnabled();
        StartMinimizedToTray = s.StartMinimizedToTray;
        MinimizeToTray = s.MinimizeToTray;
        CloseToTray = s.CloseToTray;
        GlobalHotkey = s.GlobalHotkey;

        var ver = GithubUpdateService.GetCurrentVersion();
        CurrentAppVersion = $"v{ver.Major}.{ver.Minor}.{ver.Build}";
        BackupFrequency = s.BackupFrequency;
        MaxBackupCount = s.MaxBackupCount;
        DataDirectory = _settingsService.GetDataDirectory();
        IsPortableMode = _settingsService.IsPortableMode;

        await RefreshRootsAsync();
        RefreshBackupsList();
    }

    partial void OnGlobalHotkeyChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            HotkeyStatus = "⚠ 快捷键不能为空";
            IsHotkeyValid = false;
            return;
        }

        if (WindowsHotkeyService.IsMouseKey(value))
        {
            HotkeyStatus = "ℹ 包含鼠标按键（保存时将提示确认，已解除强制阻断）";
            IsHotkeyValid = true;
            return;
        }

        var available = _hotkeyService.TestHotkeyAvailable(value);
        HotkeyStatus = available ? "✓ 快捷键可用" : "⚠ 可能已被其他软件占用（保存时将提示确认，已解除强制阻断）";
        IsHotkeyValid = available;
    }

    private async Task RefreshRootsAsync()
    {
        var roots = await _rootRepository.GetAllAsync();
        RootDirectories.Clear();
        foreach (var r in roots)
        {
            RootDirectories.Add(r);
        }
    }

    private void RefreshBackupsList()
    {
        var backups = _backupService.GetExistingBackups();
        ExistingBackups.Clear();
        foreach (var b in backups)
        {
            ExistingBackups.Add(Path.GetFileName(b));
        }
    }

    partial void OnThemeChanged(string value)
    {
        _themeService.ApplyTheme(value);
    }

    [RelayCommand]
    public void OpenDataDirectory()
    {
        if (Directory.Exists(DataDirectory))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{DataDirectory}\"",
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public void OpenLogsDirectory()
    {
        var logsDir = _settingsService.GetLogsDirectory();
        if (Directory.Exists(logsDir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{logsDir}\"",
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public async Task AddRootDirectoryAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择便携软件根目录"
        };

        if (dialog.ShowDialog() == true)
        {
            var folder = dialog.FolderName;
            if (RootDirectories.Any(r => r.Path.Equals(folder, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("该根目录已存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var root = new RootDirectory
            {
                Name = Path.GetFileName(folder),
                Path = folder
            };
            if (string.IsNullOrEmpty(root.Name)) root.Name = folder;

            await _rootRepository.AddAsync(root);
            await RefreshRootsAsync();
        }
    }

    [RelayCommand]
    public async Task DeleteRootDirectoryAsync(RootDirectory? root)
    {
        if (root == null) return;
        await _rootRepository.DeleteAsync(root.Id);
        await RefreshRootsAsync();
    }

    [RelayCommand]
    public async Task CreateBackupAsync()
    {
        try
        {
            var path = await _backupService.CreateBackupAsync();
            RefreshBackupsList();
            MessageBox.Show($"备份成功！\n文件已保存至：\n{path}", "备份成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task RestoreBackupAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要恢复的 PortableHub 备份文件",
            Filter = "PortableHub 备份 (*.phbackup)|*.phbackup|所有文件 (*.*)|*.*",
            InitialDirectory = _settingsService.GetBackupsDirectory()
        };

        if (dialog.ShowDialog() == true)
        {
            var confirm = Views.ModernDialog.ShowConfirm(
                "恢复备份将覆盖当前的数据与设置。\n系统将在恢复前自动为您创建一份当前数据的安全备份。\n\n是否继续？",
                "确认恢复备份",
                isDestructive: false);

            if (confirm)
            {
                var success = await _backupService.RestoreBackupAsync(dialog.FileName);
                if (success)
                {
                    Views.ModernDialog.ShowAlert("数据恢复成功！请点击确定重新载入数据。", "恢复完成");
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Views.ModernDialog.ShowError("备份文件损坏或无效，无法恢复。", "错误");
                }
            }
        }
    }

    [RelayCommand]
    public async Task ExportJsonAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "导出软件数据为 JSON",
            Filter = "JSON 文件 (*.json)|*.json",
            FileName = $"PortableHub_Export_{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            await _backupService.ExportJsonAsync(dialog.FileName);
            MessageBox.Show("软件数据已成功导出！", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    public async Task ImportJsonAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入软件数据 JSON",
            Filter = "JSON 文件 (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var count = await _backupService.ImportJsonAsync(dialog.FileName);
            MessageBox.Show($"成功导入 {count} 个软件！", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    public async Task SaveAndCloseAsync()
    {
        if (!string.IsNullOrWhiteSpace(GlobalHotkey))
        {
            var isMouse = WindowsHotkeyService.IsMouseKey(GlobalHotkey);
            var isAvailable = _hotkeyService.TestHotkeyAvailable(GlobalHotkey);

            if (isMouse || !isAvailable)
            {
                string promptMsg = isMouse
                    ? $"检测到快捷键【{GlobalHotkey}】包含鼠标按键。\n受 Windows 原生热键机制限制，鼠标按键可能无法全局拦截或与系统行为冲突。\n\n是否仍然确认保存并使用此快捷键？"
                    : $"检测到快捷键【{GlobalHotkey}】可能已被系统或其他软件占用，或为特殊按键组合。\n\n是否仍然确认保存并使用此快捷键？";

                var confirm = Views.ModernDialog.ShowConfirm(promptMsg, "快捷键确认提示", isDestructive: false);
                if (!confirm)
                {
                    return; // 用户取消保存，保留在设置界面方便微调
                }
            }
        }

        var s = _settingsService.CurrentSettings;
        s.Theme = Theme;
        s.Language = Language;
        s.CardSize = CardSize;
        s.ViewMode = ViewMode;
        s.LaunchClickMode = LaunchClickMode;
        s.StartWithWindows = StartWithWindows;
        s.StartMinimizedToTray = StartMinimizedToTray;
        s.MinimizeToTray = MinimizeToTray;
        s.CloseToTray = CloseToTray;
        s.GlobalHotkey = GlobalHotkey;
        s.BackupFrequency = BackupFrequency;
        s.MaxBackupCount = Math.Max(1, MaxBackupCount);

        await _settingsService.SaveSettingsAsync();

        // Apply updated global hotkey dynamically in real-time
        if (!string.IsNullOrWhiteSpace(GlobalHotkey))
        {
            _hotkeyService.Register(GlobalHotkey, IntPtr.Zero);
        }

        // Apply startup setting
        _startupService.SetAutoStart(StartWithWindows, StartMinimizedToTray);

        _originalTheme = Theme;
        _originalLanguage = Language;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    partial void OnLanguageChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _localizationService.SetLanguage(value);
        }
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        if (IsCheckingUpdates) return;
        IsCheckingUpdates = true;
        UpdateStatusMessage = _localizationService.GetString("Loc_Updates_Checking", "正在检查更新...");
        try
        {
            var info = await _updateService.CheckForUpdatesAsync();
            LatestUpdateInfo = info;
            HasNewUpdate = info.HasUpdate;
            if (info.HasUpdate)
            {
                UpdateStatusMessage = $"{_localizationService.GetString("Loc_Updates_Found", "发现新版本")}: {info.LatestVersion}";
            }
            else if (!string.IsNullOrWhiteSpace(info.ErrorMessage))
            {
                UpdateStatusMessage = info.ErrorMessage;
            }
            else
            {
                UpdateStatusMessage = _localizationService.GetString("Loc_Updates_Latest", "✓ 当前已是最新版本");
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"{_localizationService.GetString("Loc_Updates_Failed", "检查更新失败")}: {ex.Message}";
        }
        finally
        {
            IsCheckingUpdates = false;
        }
    }

    [RelayCommand]
    public void OpenReleaseUrl()
    {
        var url = LatestUpdateInfo?.ReleaseUrl ?? "https://github.com/candy-blue/PortableHub/releases";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Fallback
        }
    }

    public void RollbackThemePreview()
    {
        if (_themeService.CurrentTheme != _originalTheme)
        {
            _themeService.ApplyTheme(_originalTheme);
        }
        if (_localizationService.CurrentLanguage != _originalLanguage)
        {
            _localizationService.SetLanguage(_originalLanguage);
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        RollbackThemePreview();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
