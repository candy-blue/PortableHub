using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public partial class SoftwareCardViewModel : ObservableObject
{
    private readonly ILaunchService _launchService;
    private readonly ISoftwareRepository _softwareRepository;
    private readonly Action<SoftwareCardViewModel> _onEdit;
    private readonly Action<SoftwareCardViewModel> _onDelete;
    private readonly Action<SoftwareCardViewModel> _onRelocate;
    private readonly Action<SoftwareCardViewModel>? _onFavoriteChanged;
    private readonly Action<SoftwareCardViewModel>? _onLaunched;

    public Software Model { get; }

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isMissing;

    [ObservableProperty]
    private string? _iconPath;

    public int Id => Model.Id;
    public string Name => Model.Name;
    public string ExePath => Model.ExePath;
    public string CategoryName => Model.CategoryName;
    public int CategoryId => Model.CategoryId;
    public string? Description => Model.Description;
    public string? Tags => Model.Tags;
    public int LaunchCount => Model.LaunchCount;

    public string LastLaunchedDisplay
    {
        get
        {
            if (!Model.LastLaunchedAt.HasValue) return "从未启动";
            var span = DateTime.UtcNow - Model.LastLaunchedAt.Value;
            if (span.TotalMinutes < 1) return "刚刚";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} 分钟前";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} 小时前";
            if (span.TotalDays < 2) return "昨天";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} 天前";
            return Model.LastLaunchedAt.Value.ToLocalTime().ToString("yyyy-MM-dd");
        }
    }

    public SoftwareCardViewModel(
        Software model,
        ILaunchService launchService,
        ISoftwareRepository softwareRepository,
        Action<SoftwareCardViewModel> onEdit,
        Action<SoftwareCardViewModel> onDelete,
        Action<SoftwareCardViewModel> onRelocate,
        Action<SoftwareCardViewModel>? onFavoriteChanged = null,
        Action<SoftwareCardViewModel>? onLaunched = null)
    {
        Model = model;
        _launchService = launchService;
        _softwareRepository = softwareRepository;
        _onEdit = onEdit;
        _onDelete = onDelete;
        _onRelocate = onRelocate;
        _onFavoriteChanged = onFavoriteChanged;
        _onLaunched = onLaunched;

        _isFavorite = model.IsFavorite;
        _iconPath = model.IconPath;
        _isMissing = !File.Exists(model.ExePath);
        _isRunning = _launchService.IsRunning(model);
    }

    public void RefreshState()
    {
        IsMissing = !File.Exists(Model.ExePath);
        IsRunning = _launchService.IsRunning(Model);
        OnPropertyChanged(nameof(LaunchCount));
        OnPropertyChanged(nameof(LastLaunchedDisplay));
    }

    [RelayCommand]
    public async Task LaunchAsync()
    {
        if (IsMissing)
        {
            MessageBox.Show($"软件路径不存在：\n{Model.ExePath}\n\n请点击卡片右下角菜单选择【重新定位】。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = await _launchService.LaunchAsync(Model);
        if (!result.Success)
        {
            MessageBox.Show($"启动失败：\n{result.ErrorMessage}", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            RefreshState();
            _onLaunched?.Invoke(this);
        }
    }

    [RelayCommand]
    public async Task LaunchAsAdminAsync()
    {
        if (IsMissing)
        {
            MessageBox.Show($"软件路径不存在：\n{Model.ExePath}", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var originalAdmin = Model.RunAsAdmin;
        Model.RunAsAdmin = true;
        var result = await _launchService.LaunchAsync(Model);
        Model.RunAsAdmin = originalAdmin;

        if (!result.Success)
        {
            MessageBox.Show($"管理员启动失败：\n{result.ErrorMessage}", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            RefreshState();
            _onLaunched?.Invoke(this);
        }
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        IsFavorite = !IsFavorite;
        Model.IsFavorite = IsFavorite;
        await _softwareRepository.UpdateFavoriteAsync(Model.Id, IsFavorite);
        _onFavoriteChanged?.Invoke(this);
    }

    [RelayCommand]
    public void OpenFolder()
    {
        if (string.IsNullOrWhiteSpace(Model.ExePath)) return;
        var folder = Path.GetDirectoryName(Model.ExePath);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{Model.ExePath}\"",
                UseShellExecute = true
            });
        }
        else
        {
            MessageBox.Show("所在文件夹不存在。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    public void CopyPath()
    {
        if (!string.IsNullOrWhiteSpace(Model.ExePath))
        {
            Clipboard.SetText(Model.ExePath);
        }
    }

    [RelayCommand]
    public void CreateDesktopShortcut()
    {
        if (string.IsNullOrWhiteSpace(Model.ExePath) || !File.Exists(Model.ExePath))
        {
            MessageBox.Show("软件路径不存在，无法创建快捷方式。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var workingDir = !string.IsNullOrWhiteSpace(Model.WorkingDirectory) 
            ? Model.WorkingDirectory 
            : Path.GetDirectoryName(Model.ExePath);

        var success = PortableHub.Infrastructure.Windows.ShortcutHelper.CreateDesktopShortcut(
            Model.ExePath, 
            Model.Name, 
            Model.Arguments, 
            workingDir, 
            Model.IconPath);

        if (success)
        {
            MessageBox.Show($"已在桌面创建【{Model.Name}】的快捷方式。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("创建桌面快捷方式失败。", "Portable Hub", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void Edit() => _onEdit(this);

    [RelayCommand]
    public void Delete() => _onDelete(this);

    [RelayCommand]
    public void Relocate() => _onRelocate(this);
}

