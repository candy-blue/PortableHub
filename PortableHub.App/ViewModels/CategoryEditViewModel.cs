using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortableHub.Core.Models;
using Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol;

namespace PortableHub.App.ViewModels;

public record CategoryIconOption(Symbol Symbol, string Name, string Key);

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
    private Symbol _selectedSymbol = Symbol.Folder;

    [ObservableProperty]
    private int _sortOrder = 0;

    public List<string> AvailableColors { get; } =
    [
        "#0078D4", "#0284C7", "#06B6D4", "#0D9488", "#10B981", "#059669",
        "#6366F1", "#8B5CF6", "#7C3AED", "#A855F7", "#D946EF", "#EC4899",
        "#F43F5E", "#EF4444", "#F97316", "#EA580C", "#F59E0B", "#D97706",
        "#84CC16", "#65A30D", "#14B8A6", "#64748B", "#475569", "#1E293B"
    ];

    public List<CategoryIconOption> AvailableIconOptions { get; } =
    [
        new(Symbol.AllApps, "综合应用", "AllApps"),
        new(Symbol.Folder, "文件夹", "Folder"),
        new(Symbol.Repair, "系统维护", "Repair"),
        new(Symbol.Globe, "网络工具", "Globe"),
        new(Symbol.Document, "办公文档", "Document"),
        new(Symbol.Pictures, "图形图像", "Pictures"),
        new(Symbol.Play, "影音播放", "Play"),
        new(Symbol.Permissions, "安全防御", "Permissions"),
        new(Symbol.Library, "数据库", "Library"),
        new(Symbol.XboxOneConsole, "游戏娱乐", "XboxOneConsole"),
        new(Symbol.Audio, "音频音乐", "Audio"),
        new(Symbol.Remote, "桌面环境", "Remote"),
        new(Symbol.Upload, "云端存储", "Upload"),
        new(Symbol.Tag, "标签分类", "Tag"),
        new(Symbol.Favorite, "精选常用", "Favorite"),
        new(Symbol.Like, "特别喜爱", "Like"),
        new(Symbol.Read, "阅读学习", "Read"),
        new(Symbol.Camera, "摄影截图", "Camera"),
        new(Symbol.Message, "社交通讯", "Message"),
        new(Symbol.Edit, "文本写作", "Edit"),
        new(Symbol.Send, "启动提速", "Send"),
        new(Symbol.Setting, "系统设置", "Setting"),
        new(Symbol.SaveLocal, "便携存储", "SaveLocal"),
        new(Symbol.Download, "下载传输", "Download"),
        new(Symbol.Find, "检索查询", "Find"),
        new(Symbol.Calculator, "计算工具", "Calculator"),
        new(Symbol.Clock, "时间历史", "Clock"),
        new(Symbol.Scan, "扫描检测", "Scan"),
        new(Symbol.Link, "快捷链接", "Link"),
        new(Symbol.Mail, "邮件网络", "Mail")
    ];

    [ObservableProperty]
    private CategoryIconOption? _selectedIconOption;

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

        _selectedSymbol = Helpers.SymbolHelper.ResolveSymbol(_icon, Symbol.Folder);

        _selectedIconOption = AvailableIconOptions.FirstOrDefault(o => o.Symbol == _selectedSymbol) 
                              ?? AvailableIconOptions.FirstOrDefault(o => o.Key.Equals(_icon, StringComparison.OrdinalIgnoreCase))
                              ?? AvailableIconOptions[1]; // Folder
    }

    partial void OnSelectedIconOptionChanged(CategoryIconOption? value)
    {
        if (value != null)
        {
            SelectedSymbol = value.Symbol;
            Icon = value.Key;
        }
    }

    partial void OnSelectedSymbolChanged(Symbol value)
    {
        Icon = value.ToString();
    }

    [RelayCommand]
    public void SelectColor(string color)
    {
        Color = color;
    }

    [RelayCommand]
    public void SelectIconOption(CategoryIconOption option)
    {
        SelectedIconOption = option;
        SelectedSymbol = option.Symbol;
        Icon = option.Key;
    }

    [RelayCommand]
    public void OpenColorPicker()
    {
        var picked = Views.ColorPickerDialog.PickColor(Color);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            Color = picked;
        }
    }

    [RelayCommand]
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            Views.ModernDialog.ShowWarning("请输入分类名称。", "提示");
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
