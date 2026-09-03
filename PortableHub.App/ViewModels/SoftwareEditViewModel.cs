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
