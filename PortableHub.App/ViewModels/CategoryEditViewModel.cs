using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PortableHub.Core.Models;

namespace PortableHub.App.ViewModels;

public record CategoryIconOption(Wpf.Ui.Controls.SymbolRegular Symbol, string Name, string Key);

public partial class CategoryEditViewModel : ObservableObject
{
    public Category? ResultCategory { get; private set; }
    public bool IsEditMode { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = "#3B82F6";

    [ObservableProperty]
    private string _icon = "Folder24";

    [ObservableProperty]
    private Wpf.Ui.Controls.SymbolRegular _selectedSymbol = Wpf.Ui.Controls.SymbolRegular.Folder24;

    [ObservableProperty]
    private int _sortOrder = 0;

    public List<string> AvailableColors { get; } =
    [
        "#3B82F6", "#10B981", "#6366F1", "#F59E0B", 
        "#EC4899", "#8B5CF6", "#06B6D4", "#EF4444", 
        "#6B7280", "#14B8A6", "#F97316", "#84CC16"
    ];

    public List<CategoryIconOption> AvailableIconOptions { get; } =
    [
        new(Wpf.Ui.Controls.SymbolRegular.Apps24, "综合应用", "Apps24"),
        new(Wpf.Ui.Controls.SymbolRegular.Folder24, "文件夹", "Folder24"),
        new(Wpf.Ui.Controls.SymbolRegular.Code24, "代码开发", "Code24"),
        new(Wpf.Ui.Controls.SymbolRegular.Wrench24, "系统维护", "Wrench24"),
        new(Wpf.Ui.Controls.SymbolRegular.Globe24, "网络工具", "Globe24"),
        new(Wpf.Ui.Controls.SymbolRegular.Document24, "办公文档", "Document24"),
        new(Wpf.Ui.Controls.SymbolRegular.Image24, "图形图像", "Image24"),
        new(Wpf.Ui.Controls.SymbolRegular.Play24, "影音播放", "Play24"),
        new(Wpf.Ui.Controls.SymbolRegular.Shield24, "安全防御", "Shield24"),
        new(Wpf.Ui.Controls.SymbolRegular.Database24, "数据库", "Database24"),
        new(Wpf.Ui.Controls.SymbolRegular.Games24, "游戏娱乐", "Games24"),
        new(Wpf.Ui.Controls.SymbolRegular.MusicNote224, "音频音乐", "MusicNote224"),
        new(Wpf.Ui.Controls.SymbolRegular.Desktop24, "桌面环境", "Desktop24"),
        new(Wpf.Ui.Controls.SymbolRegular.Cloud24, "云端存储", "Cloud24"),
        new(Wpf.Ui.Controls.SymbolRegular.Tag24, "标签分类", "Tag24"),
        new(Wpf.Ui.Controls.SymbolRegular.Star24, "精选常用", "Star24"),
        new(Wpf.Ui.Controls.SymbolRegular.Heart24, "特别喜爱", "Heart24"),
        new(Wpf.Ui.Controls.SymbolRegular.Book24, "阅读学习", "Book24"),
        new(Wpf.Ui.Controls.SymbolRegular.Camera24, "摄影截图", "Camera24"),
        new(Wpf.Ui.Controls.SymbolRegular.Chat24, "社交通讯", "Chat24"),
        new(Wpf.Ui.Controls.SymbolRegular.Edit24, "文本写作", "Edit24"),
        new(Wpf.Ui.Controls.SymbolRegular.Rocket24, "启动提速", "Rocket24"),
        new(Wpf.Ui.Controls.SymbolRegular.Toolbox24, "日常工具", "Toolbox24"),
        new(Wpf.Ui.Controls.SymbolRegular.UsbStick24, "便携存储", "UsbStick24"),
        new(Wpf.Ui.Controls.SymbolRegular.Sparkle24, "智能AI", "Sparkle24"),
        new(Wpf.Ui.Controls.SymbolRegular.Cube24, "容器杂项", "Cube24"),
        new(Wpf.Ui.Controls.SymbolRegular.LockClosed24, "隐私加密", "LockClosed24"),
        new(Wpf.Ui.Controls.SymbolRegular.WeatherSunny24, "生活日常", "WeatherSunny24"),
        new(Wpf.Ui.Controls.SymbolRegular.CompassNorthwest24, "网页浏览", "CompassNorthwest24"),
        new(Wpf.Ui.Controls.SymbolRegular.Mail24, "邮件收发", "Mail24"),
        new(Wpf.Ui.Controls.SymbolRegular.ArrowDownload24, "下载传输", "ArrowDownload24"),
        new(Wpf.Ui.Controls.SymbolRegular.Window24, "系统窗口", "Window24"),
        new(Wpf.Ui.Controls.SymbolRegular.PaintBrush24, "设计绘图", "PaintBrush24"),
        new(Wpf.Ui.Controls.SymbolRegular.Settings24, "系统设置", "Settings24"),
        new(Wpf.Ui.Controls.SymbolRegular.HardDrive24, "磁盘存储", "HardDrive24"),
        new(Wpf.Ui.Controls.SymbolRegular.Search24, "检索查询", "Search24")
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
            _icon = existing.Icon ?? "Folder24";
            _sortOrder = existing.SortOrder;
        }

        if (Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(_icon, true, out var sym))
        {
            _selectedSymbol = sym;
        }
        else if (Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(_icon + "24", true, out var sym24))
        {
            _selectedSymbol = sym24;
        }
        else
        {
            _selectedSymbol = Wpf.Ui.Controls.SymbolRegular.Folder24;
        }

        _selectedIconOption = AvailableIconOptions.FirstOrDefault(o => o.Symbol == _selectedSymbol) 
                              ?? AvailableIconOptions.FirstOrDefault(o => o.Key.Equals(_icon, StringComparison.OrdinalIgnoreCase))
                              ?? AvailableIconOptions[1]; // Folder24
    }

    partial void OnSelectedIconOptionChanged(CategoryIconOption? value)
    {
        if (value != null)
        {
            SelectedSymbol = value.Symbol;
            Icon = value.Key;
        }
    }

    partial void OnSelectedSymbolChanged(Wpf.Ui.Controls.SymbolRegular value)
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
        SelectedSymbol = option.Symbol;
        Icon = option.Key;
    }

    [RelayCommand]
    public void OpenColorPicker()
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            AllowFullOpen = true,
            AnyColor = true
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(Color))
            {
                dialog.Color = System.Drawing.ColorTranslator.FromHtml(Color);
            }
        }
        catch { }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            Color = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
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
