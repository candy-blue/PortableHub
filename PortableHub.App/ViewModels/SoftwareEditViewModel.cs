using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public partial class SoftwareEditViewModel : ObservableObject
{
    private readonly IIconService _iconService;
    private readonly IFileScannerService _scannerService;
    private readonly ISoftwareRepository _softwareRepository;
    private readonly ICategoryRepository _categoryRepository;

    public bool IsEditMode { get; }
    public Software Model { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _exePath = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _arguments = string.Empty;

    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private string? _iconPath;

    [ObservableProperty]
    private bool _runAsAdmin;

    [ObservableProperty]
    private bool _singleInstance = true;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<Software> LinkedApps { get; } = [];
    public ObservableCollection<Software> AvailableCandidates { get; } = [];

    [ObservableProperty]
    private Software? _selectedCandidateToAdd;

    public bool CanAddMoreLinkedApps => LinkedApps.Count < 5;

    private List<Software> _allCachedSoftware = [];

    public event EventHandler<bool>? RequestClose;

    public SoftwareEditViewModel(
        Software model,
        bool isEditMode,
        IIconService iconService,
        IFileScannerService scannerService,
        ISoftwareRepository softwareRepository,
        ICategoryRepository categoryRepository)
    {
        Model = model;
        IsEditMode = isEditMode;
        _iconService = iconService;
        _scannerService = scannerService;
        _softwareRepository = softwareRepository;
        _categoryRepository = categoryRepository;

        _name = model.Name;
        _exePath = model.ExePath;
        _arguments = model.Arguments ?? string.Empty;
        _workingDirectory = model.WorkingDirectory ?? string.Empty;
        _description = model.Description ?? string.Empty;
        _tags = model.Tags ?? string.Empty;
        _iconPath = model.IconPath;
        _runAsAdmin = model.RunAsAdmin;
        _singleInstance = model.SingleInstance;
    }

    public async Task InitializeAsync()
    {
        var cats = await _categoryRepository.GetAllAsync();
        Categories.Clear();
        foreach (var c in cats)
        {
            Categories.Add(c);
        }

        SelectedCategory = Categories.FirstOrDefault(c => c.Id == Model.CategoryId) 
                           ?? Categories.FirstOrDefault();

        // Load software candidates and existing linked apps
        var allSoftware = await _softwareRepository.GetAllAsync();
        _allCachedSoftware = allSoftware.ToList();

        var linkedIds = Model.GetLinkedSoftwareIdList();
        LinkedApps.Clear();
        foreach (var id in linkedIds)
        {
            var s = _allCachedSoftware.FirstOrDefault(x => x.Id == id);
            if (s != null)
            {
                LinkedApps.Add(s);
            }
        }

        UpdateAvailableCandidates();
    }

    private void UpdateAvailableCandidates()
    {
        AvailableCandidates.Clear();
        var linkedIdSet = new HashSet<int>(LinkedApps.Select(s => s.Id));
        if (Model.Id > 0)
        {
            linkedIdSet.Add(Model.Id); // Do not allow linking self
        }

        foreach (var s in _allCachedSoftware.Where(x => !linkedIdSet.Contains(x.Id)).OrderBy(x => x.Name))
        {
            AvailableCandidates.Add(s);
        }

        OnPropertyChanged(nameof(CanAddMoreLinkedApps));
    }

    partial void OnSelectedCandidateToAddChanged(Software? value)
    {
        if (value != null && LinkedApps.Count < 5)
        {
            LinkedApps.Add(value);
            _selectedCandidateToAdd = null;
            OnPropertyChanged(nameof(SelectedCandidateToAdd));
            UpdateAvailableCandidates();
        }
    }

    [RelayCommand]
    public void RemoveLinkedApp(Software? app)
    {
        if (app != null && LinkedApps.Contains(app))
        {
            LinkedApps.Remove(app);
            UpdateAvailableCandidates();
        }
    }

    [RelayCommand]
    public async Task BrowseExeAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择软件可执行文件",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*",
            FileName = ExePath
        };

        if (dialog.ShowDialog() == true)
        {
            ExePath = dialog.FileName;

            if (string.IsNullOrWhiteSpace(Name))
            {
                var candidate = _scannerService.AnalyzeExe(ExePath, Categories.ToList());
                Name = candidate.DeducedName;
                if (SelectedCategory == null)
                {
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == candidate.CategoryId);
                }
            }

            if (string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                WorkingDirectory = Path.GetDirectoryName(ExePath) ?? string.Empty;
            }

            var icon = await _iconService.ExtractAndCacheIconAsync(ExePath);
            if (icon != null)
            {
                IconPath = icon;
            }
        }
    }

    [RelayCommand]
    public async Task BrowseIconAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择自定义图标",
            Filter = "图像文件 (*.png;*.ico;*.jpg)|*.png;*.ico;*.jpg;*.jpeg|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var saved = await _iconService.SaveCustomIconAsync(dialog.FileName);
            if (saved != null)
            {
                IconPath = saved;
            }
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            System.Windows.MessageBox.Show("请输入软件名称。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ExePath))
        {
            System.Windows.MessageBox.Show("请选择可执行文件路径。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        Model.Name = Name.Trim();
        Model.ExePath = ExePath.Trim();
        Model.CategoryId = SelectedCategory?.Id ?? 1;
        Model.Arguments = string.IsNullOrWhiteSpace(Arguments) ? null : Arguments.Trim();
        Model.WorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory) ? null : WorkingDirectory.Trim();
        Model.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        Model.Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags.Trim();
        Model.IconPath = IconPath;
        Model.RunAsAdmin = RunAsAdmin;
        Model.SingleInstance = SingleInstance;
        Model.LinkedSoftwareIds = LinkedApps.Count > 0
            ? string.Join(",", LinkedApps.Select(s => s.Id))
            : null;

        if (IsEditMode)
        {
            await _softwareRepository.UpdateAsync(Model);
        }
        else
        {
            await _softwareRepository.AddAsync(Model);
        }

        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }
}
