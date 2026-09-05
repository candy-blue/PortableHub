using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public class CategoryNavModel : ObservableObject
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string NavMode { get; set; } = "Category"; // All, Favorite, Recent, Category
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }

    public string Glyph => NavMode switch
    {
        "All" => "\uE80F",
        "Favorite" => "\uE735",
        "Recent" => "\uEC92",
        _ => "\uE8B7"
    };

    public Wpf.Ui.Controls.SymbolRegular Symbol
    {
        get
        {
            if (NavMode == "All") return Wpf.Ui.Controls.SymbolRegular.Home24;
            if (NavMode == "Favorite") return Wpf.Ui.Controls.SymbolRegular.Star24;
            if (NavMode == "Recent") return Wpf.Ui.Controls.SymbolRegular.History24;

            if (!string.IsNullOrWhiteSpace(Icon))
            {
                if (Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(Icon, true, out var exact))
                    return exact;
                if (Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(Icon + "24", true, out var with24))
                    return with24;
            }

            return Wpf.Ui.Controls.SymbolRegular.Tag24;
        }
    }

    private int _count;
    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }
}

public partial class MainViewModel : ObservableObject
{
    private readonly ISoftwareRepository _softwareRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IRootDirectoryRepository _rootRepository;
    private readonly ILaunchService _launchService;
    private readonly IIconService _iconService;
    private readonly ISearchService _searchService;
    private readonly IFileScannerService _scannerService;
    private readonly IPathRepairService _pathRepairService;
    private readonly ISettingsService _settingsService;

    // View interaction callbacks
    public Func<Software, Task<bool>>? ShowSoftwareEditDialog { get; set; }
    public Func<Category?, Task<Category?>>? ShowCategoryEditDialog { get; set; }
    public Func<Task>? ShowScannerDialog { get; set; }
    public Func<Task>? ShowSettingsDialog { get; set; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _cardSize = "Medium";

    [ObservableProperty]
    private string _sortBy = "Custom";

    [ObservableProperty]
    private CategoryNavModel? _selectedNav;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalSoftwareCount;

    [ObservableProperty]
    private SoftwareCardViewModel? _selectedSoftware;

    public ObservableCollection<CategoryNavModel> NavItems { get; } = [];
    public ObservableCollection<CategoryNavModel> CustomCategories { get; } = [];
    public ObservableCollection<SoftwareCardViewModel> FilteredSoftware { get; } = [];

    private readonly List<SoftwareCardViewModel> _allSoftwareCards = [];
    private CancellationTokenSource? _reorderDebounceCts;

    public MainViewModel(
        ISoftwareRepository softwareRepository,
        ICategoryRepository categoryRepository,
        IRootDirectoryRepository rootRepository,
        ILaunchService launchService,
        IIconService iconService,
        ISearchService searchService,
        IFileScannerService scannerService,
        IPathRepairService pathRepairService,
        ISettingsService settingsService)
    {
        _softwareRepository = softwareRepository;
        _categoryRepository = categoryRepository;
        _rootRepository = rootRepository;
        _launchService = launchService;
        _iconService = iconService;
        _searchService = searchService;
        _scannerService = scannerService;
        _pathRepairService = pathRepairService;
        _settingsService = settingsService;

        _cardSize = _settingsService.CurrentSettings.CardSize;
        _sortBy = _settingsService.CurrentSettings.SortBy;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await RefreshDataAsync();

            // Set default nav to 'All'
            SelectedNav = NavItems.FirstOrDefault(n => n.NavMode == "All");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedNavChanged(CategoryNavModel? value)
    {
        ApplyFilter();
    }

    partial void OnCardSizeChanged(string value)
    {
        _settingsService.CurrentSettings.CardSize = value;
        _ = _settingsService.SaveSettingsAsync();
    }

    partial void OnSortByChanged(string value)
    {
        _settingsService.CurrentSettings.SortBy = value;
        _ = _settingsService.SaveSettingsAsync();
        ApplyFilter();
    }

    public async Task RefreshDataAsync()
    {
        var rawCategories = await _categoryRepository.GetAllAsync();
        var rawSoftware = await _softwareRepository.GetAllAsync();
        var roots = await _rootRepository.GetAllAsync();

        // Check if missing paths can be auto-repaired
        if (_settingsService.CurrentSettings.AutoRelocateMissing)
        {
            foreach (var s in rawSoftware)
            {
                if (!File.Exists(s.ExePath))
                {
                    await _pathRepairService.TryAutoRepairPathAsync(s, roots);
                }
            }
        }

        // Build Nav Items (All, Favorites, Recent)
        NavItems.Clear();
        NavItems.Add(new CategoryNavModel { Name = "全部软件", Icon = "Grid", NavMode = "All", Color = "#3B82F6", Count = rawSoftware.Count });
        NavItems.Add(new CategoryNavModel { Name = "我的收藏", Icon = "Star", NavMode = "Favorite", Color = "#F59E0B", Count = rawSoftware.Count(s => s.IsFavorite) });
        NavItems.Add(new CategoryNavModel { Name = "最近使用", Icon = "Clock", NavMode = "Recent", Color = "#10B981", Count = rawSoftware.Count(s => s.LastLaunchedAt.HasValue) });

        // Custom categories
        CustomCategories.Clear();
        foreach (var cat in rawCategories)
        {
            var count = rawSoftware.Count(s => s.CategoryId == cat.Id);
            var item = new CategoryNavModel
            {
                Id = cat.Id,
                Name = cat.Name,
                Icon = cat.Icon ?? "Folder",
                Color = cat.Color ?? "#3B82F6",
                NavMode = "Category",
                IsSystem = cat.IsSystem,
                SortOrder = cat.SortOrder,
                Count = count
            };
            CustomCategories.Add(item);
        }

        // Build software cards
        _allSoftwareCards.Clear();
        foreach (var s in rawSoftware)
        {
            var card = new SoftwareCardViewModel(
                s,
                _launchService,
                _softwareRepository,
                OnEditSoftware,
                OnDeleteSoftware,
                OnRelocateSoftware
            );
            _allSoftwareCards.Add(card);
        }

        TotalSoftwareCount = _allSoftwareCards.Count;
        _searchService.IndexSoftware(rawSoftware);

        ApplyFilter();
    }

    public void ApplyFilter()
    {
        IEnumerable<SoftwareCardViewModel> query = _allSoftwareCards;

        // Navigation filter
        if (SelectedNav != null)
        {
            if (SelectedNav.NavMode == "Favorite")
            {
                query = query.Where(c => c.IsFavorite);
            }
            else if (SelectedNav.NavMode == "Recent")
            {
                query = query.Where(c => c.Model.LastLaunchedAt.HasValue);
            }
            else if (SelectedNav.NavMode == "Category" && SelectedNav.Id.HasValue)
            {
                query = query.Where(c => c.CategoryId == SelectedNav.Id.Value);
            }
        }

        // Search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var tokens = SearchText.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(c =>
            {
                var name = c.Name.ToLowerInvariant();
                var cat = c.CategoryName.ToLowerInvariant();
                var tags = (c.Tags ?? string.Empty).ToLowerInvariant();
                var desc = (c.Description ?? string.Empty).ToLowerInvariant();
                return tokens.All(t => name.Contains(t) || cat.Contains(t) || tags.Contains(t) || desc.Contains(t));
            });
        }

        // Sorting
        query = SortBy switch
        {
            "NameAsc" => query.OrderBy(c => c.Name),
            "NameDesc" => query.OrderByDescending(c => c.Name),
            "RecentUsed" => query.OrderByDescending(c => c.Model.LastLaunchedAt ?? DateTime.MinValue),
            "LaunchCount" => query.OrderByDescending(c => c.LaunchCount),
            "CreatedAt" => query.OrderByDescending(c => c.Model.CreatedAt),
            _ => query.OrderBy(c => c.Model.SortOrder).ThenBy(c => c.Id)
        };

        FilteredSoftware.Clear();
        foreach (var item in query)
        {
            FilteredSoftware.Add(item);
        }
    }

    [RelayCommand]
    public async Task AddSoftwareAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择便携软件可执行文件",
            Filter = "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await AddSoftwareFromPathAsync(dialog.FileName);
        }
    }

    public async Task AddSoftwareFromPathAsync(string exePath, int? targetCategoryId = null)
    {
        var normalized = PortableHub.Infrastructure.Windows.ShortcutHelper.NormalizeDroppedPath(exePath);
        if (string.IsNullOrWhiteSpace(normalized) || !File.Exists(normalized))
            return;

        var categories = await _categoryRepository.GetAllAsync();
        var candidate = _scannerService.AnalyzeExe(normalized, categories);

        int assignedCategoryId;
        if (targetCategoryId.HasValue && targetCategoryId.Value > 0)
        {
            assignedCategoryId = targetCategoryId.Value;
        }
        else if (SelectedNav?.NavMode == "Category" && SelectedNav.Id.HasValue)
        {
            assignedCategoryId = SelectedNav.Id.Value;
        }
        else
        {
            assignedCategoryId = candidate.CategoryId;
        }

        // Extract icon asynchronously
        var iconPath = await _iconService.ExtractAndCacheIconAsync(normalized);

        var software = new Software
        {
            Name = candidate.DeducedName,
            ExePath = normalized,
            CategoryId = assignedCategoryId,
            Description = candidate.FileDescription,
            IconPath = iconPath,
            SortOrder = _allSoftwareCards.Count + 1
        };

        if (ShowSoftwareEditDialog != null)
        {
            var saved = await ShowSoftwareEditDialog(software);
            if (saved)
            {
                await RefreshDataAsync();
            }
        }
        else
        {
            await _softwareRepository.AddAsync(software);
            await RefreshDataAsync();
        }
    }

    private async void OnEditSoftware(SoftwareCardViewModel card)
    {
        if (ShowSoftwareEditDialog != null)
        {
            var saved = await ShowSoftwareEditDialog(card.Model);
            if (saved)
            {
                await RefreshDataAsync();
            }
        }
    }

    private async void OnDeleteSoftware(SoftwareCardViewModel card)
    {
        var result = MessageBox.Show(
            $"确定从 Portable Hub 中移除软件【{card.Name}】吗？\n\n注意：此操作绝不会删除硬盘中的软件本体文件。",
            "移除软件确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _softwareRepository.DeleteAsync(card.Id);
            await RefreshDataAsync();
        }
    }

    private async void OnRelocateSoftware(SoftwareCardViewModel card)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"为【{card.Name}】重新定位可执行文件",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            FileName = Path.GetFileName(card.ExePath)
        };

        if (dialog.ShowDialog() == true)
        {
            var newPath = dialog.FileName;
            card.Model.ExePath = newPath;
            await _softwareRepository.UpdatePathAsync(card.Id, newPath, card.Model.RootId, card.Model.RelativePath);

            // Re-extract icon if needed
            var newIcon = await _iconService.ExtractAndCacheIconAsync(newPath, card.Id);
            if (newIcon != null)
            {
                card.IconPath = newIcon;
                card.Model.IconPath = newIcon;
                await _softwareRepository.UpdateAsync(card.Model);
            }

            card.RefreshState();
            ApplyFilter();
        }
    }

    [RelayCommand]
    public async Task AddCategoryAsync()
    {
        if (ShowCategoryEditDialog != null)
        {
            var created = await ShowCategoryEditDialog(null);
            if (created != null)
            {
                await _categoryRepository.AddAsync(created);
                await RefreshDataAsync();
            }
        }
    }

    [RelayCommand]
    public async Task EditCategoryAsync(CategoryNavModel? navItem)
    {
        if (navItem == null || !navItem.Id.HasValue) return;
        var cat = await _categoryRepository.GetByIdAsync(navItem.Id.Value);
        if (cat == null) return;

        if (ShowCategoryEditDialog != null)
        {
            var edited = await ShowCategoryEditDialog(cat);
            if (edited != null)
            {
                await _categoryRepository.UpdateAsync(edited);
                await RefreshDataAsync();
            }
        }
    }

    [RelayCommand]
    public async Task DeleteCategoryAsync(CategoryNavModel? navItem)
    {
        if (navItem == null || !navItem.Id.HasValue) return;

        var allCats = await _categoryRepository.GetAllAsync();
        var fallback = allCats.FirstOrDefault(c => c.Name == "其他" && c.Id != navItem.Id.Value)
                       ?? allCats.FirstOrDefault(c => c.Id != navItem.Id.Value);

        if (fallback == null)
        {
            MessageBox.Show("至少需要保留一个分类。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定删除分类【{navItem.Name}】吗？\n该分类下的软件将被自动移动到【{fallback.Name}】。",
            "删除分类确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _categoryRepository.DeleteAsync(navItem.Id.Value, fallback.Id);
            await RefreshDataAsync();
        }
    }

    public async Task MoveSoftwareToCategoryAsync(SoftwareCardViewModel card, int targetCategoryId)
    {
        await _softwareRepository.UpdateCategoryAsync(card.Id, targetCategoryId);
        await RefreshDataAsync();
    }

    public void ReorderCards(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= FilteredSoftware.Count ||
            targetIndex < 0 || targetIndex >= FilteredSoftware.Count ||
            sourceIndex == targetIndex)
        {
            return;
        }

        var item = FilteredSoftware[sourceIndex];
        FilteredSoftware.Move(sourceIndex, targetIndex);

        // Update SortOrder in memory
        for (int i = 0; i < FilteredSoftware.Count; i++)
        {
            FilteredSoftware[i].Model.SortOrder = i + 1;
        }

        // Debounce database write (500ms)
        _reorderDebounceCts?.Cancel();
        _reorderDebounceCts = new CancellationTokenSource();
        var token = _reorderDebounceCts.Token;

        Task.Delay(500, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                var pairs = FilteredSoftware.Select((c, idx) => (c.Id, idx + 1)).ToList();
                await _softwareRepository.UpdateSortOrdersAsync(pairs);
            }
        }, TaskScheduler.Default);
    }

    private CancellationTokenSource? _reorderCategoryDebounceCts;

    public void ReorderCategories(int sourceIndex, int targetIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= CustomCategories.Count ||
            targetIndex < 0 || targetIndex >= CustomCategories.Count ||
            sourceIndex == targetIndex)
        {
            return;
        }

        var item = CustomCategories[sourceIndex];
        CustomCategories.Move(sourceIndex, targetIndex);

        for (int i = 0; i < CustomCategories.Count; i++)
        {
            CustomCategories[i].SortOrder = i + 1;
        }

        _reorderCategoryDebounceCts?.Cancel();
        _reorderCategoryDebounceCts = new CancellationTokenSource();
        var token = _reorderCategoryDebounceCts.Token;

        Task.Delay(500, token).ContinueWith(async t =>
        {
            if (!t.IsCanceled)
            {
                var pairs = CustomCategories
                    .Where(c => c.Id.HasValue)
                    .Select((c, idx) => (c.Id!.Value, idx + 1))
                    .ToList();
                await _categoryRepository.UpdateSortOrdersAsync(pairs);
            }
        }, TaskScheduler.Default);
    }

    [RelayCommand]
    public async Task OpenScannerAsync()
    {
        if (ShowScannerDialog != null)
        {
            await ShowScannerDialog();
            await RefreshDataAsync();
        }
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        if (ShowSettingsDialog != null)
        {
            await ShowSettingsDialog();
            CardSize = _settingsService.CurrentSettings.CardSize;
            SortBy = _settingsService.CurrentSettings.SortBy;
            await RefreshDataAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync() => await RefreshDataAsync();

    public void RefreshRunningStates()
    {
        foreach (var card in _allSoftwareCards)
        {
            card.RefreshState();
        }
    }
}
