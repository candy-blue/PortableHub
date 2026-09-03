using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public partial class CategoryEditViewModel : ObservableObject
{
    public Category? ResultCategory { get; private set; }
    public bool IsEditMode { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#3B82F6";

    [ObservableProperty]
    private string _icon = "Folder";

    [ObservableProperty]
    private int _sortOrder = 0;

    public List<string> AvailableColors { get; } =
    [
        "#3B82F6", "#10B981", "#6366F1", "#F59E0B", 
        "#EC4899", "#8B5CF6", "#06B6D4", "#EF4444", "#6B7280"
    ];

    public List<string> AvailableIcons { get; } =
    [
        "Folder", "Code", "Wrench", "Globe", "Document", 
        "Image", "Play", "Shield", "Cube", "Star", "Heart"
    ];

    public event EventHandler<bool>? RequestClose;

    public CategoryEditViewModel(Category? existing = null)
    {
        if (existing != null)
        {
            IsEditMode = true;
            ResultCategory = existing;
            _name = existing.Name;
            _color = existing.Color ?? "#3B82F6";
            _icon = existing.Icon ?? "Folder";
            _sortOrder = existing.SortOrder;
        }
    }

    [RelayCommand]
    public void SelectColor(string color)
    {
        Color = color;
    }

    [RelayCommand]
    public void SelectIcon(string icon)
    {
        Icon = icon;
    }

    [RelayCommand]
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            System.Windows.MessageBox.Show("请输入分类名称。", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (ResultCategory == null)
        {
            ResultCategory = new Category();
        }

        ResultCategory.Name = Name.Trim();
        ResultCategory.Color = Color;
        ResultCategory.Icon = Icon;
        ResultCategory.SortOrder = SortOrder;

        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    public void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }
}
