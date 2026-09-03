using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public partial class ScannerViewModel : ObservableObject
{
    private readonly IFileScannerService _scannerService;
    private readonly ISoftwareRepository _softwareRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IRootDirectoryRepository _rootRepository;
    private readonly IIconService _iconService;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _progressPercentage;

    [ObservableProperty]
    private string _statusText = "请选择便携软件所在目录开始扫描";

    public ObservableCollection<SoftwareScanCandidate> Candidates { get; } = [];

    public event EventHandler? RequestClose;

    public ScannerViewModel(
        IFileScannerService scannerService,
        ISoftwareRepository softwareRepository,
        ICategoryRepository categoryRepository,
        IRootDirectoryRepository rootRepository,
        IIconService iconService)
    {
        _scannerService = scannerService;
        _softwareRepository = softwareRepository;
        _categoryRepository = categoryRepository;
        _rootRepository = rootRepository;
        _iconService = iconService;
    }

    [RelayCommand]
    public void BrowseDirectory()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择便携软件根目录",
            InitialDirectory = DirectoryPath
        };

        if (dialog.ShowDialog() == true)
        {
            DirectoryPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
        {
            MessageBox.Show("请选择有效的文件夹路径。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsScanning = true;
        ProgressPercentage = 0;
        StatusText = "正在智能扫描目录中的可执行文件...";
        Candidates.Clear();

        _scanCts = new CancellationTokenSource();

        try
        {
            var categories = await _categoryRepository.GetAllAsync();
            var roots = await _rootRepository.GetAllAsync();
            var existingRoot = roots.FirstOrDefault(r => r.Path.Equals(DirectoryPath, StringComparison.OrdinalIgnoreCase));
            int? rootId = existingRoot?.Id;

            // If not registered as root directory, register it
            if (existingRoot == null)
            {
                var newRoot = new RootDirectory
                {
                    Name = Path.GetFileName(DirectoryPath),
                    Path = DirectoryPath
                };
                if (string.IsNullOrEmpty(newRoot.Name))
                {
                    newRoot.Name = DirectoryPath;
                }
                rootId = await _rootRepository.AddAsync(newRoot);
            }

            var progress = new Progress<int>(percent => ProgressPercentage = percent);
            var results = await _scannerService.ScanDirectoryAsync(DirectoryPath, categories, rootId, progress, _scanCts.Token);

            foreach (var item in results)
            {
                Candidates.Add(item);
            }

            StatusText = $"扫描完成，共发现 {Candidates.Count} 个软件。";
        }
        catch (OperationCanceledException)
        {
            StatusText = "扫描已取消。";
        }
        catch (Exception ex)
        {
            StatusText = $"扫描出错: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        _scanCts?.Cancel();
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var c in Candidates) c.IsSelected = true;
    }

    [RelayCommand]
    public void UnselectAll()
    {
        foreach (var c in Candidates) c.IsSelected = false;
    }

    [RelayCommand]
    public async Task AddSelectedAsync()
    {
        var selected = Candidates.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("未勾选任何软件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existingSoftware = await _softwareRepository.GetAllAsync();
        var existingPaths = new HashSet<string>(existingSoftware.Select(s => s.ExePath), StringComparer.OrdinalIgnoreCase);

        int addedCount = 0;
        foreach (var c in selected)
        {
            if (existingPaths.Contains(c.ExePath))
            {
                continue; // Skip already added
            }

            var iconPath = await _iconService.ExtractAndCacheIconAsync(c.ExePath);
            var sw = new Software
            {
                Name = c.DeducedName,
                ExePath = c.ExePath,
                CategoryId = c.CategoryId,
                RootId = c.RootId,
                Description = c.FileDescription,
                IconPath = iconPath,
                SortOrder = existingSoftware.Count + addedCount + 1
            };

            await _softwareRepository.AddAsync(sw);
            addedCount++;
        }

        MessageBox.Show($"成功添加 {addedCount} 个软件到管理器！", "导入完成", MessageBoxButton.OK, MessageBoxImage.Information);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
