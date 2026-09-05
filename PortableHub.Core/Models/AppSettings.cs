namespace PortableHub.Core.Models;

public class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimizedToTray { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public string GlobalHotkey { get; set; } = "Ctrl+Alt+Space";
    public string Theme { get; set; } = "System"; // System, Light, Dark
    public string CardSize { get; set; } = "Medium"; // Small, Medium, Large
    public string Language { get; set; } = "zh-CN";
    public string BackupFrequency { get; set; } = "Weekly"; // Off, Daily, Weekly, Monthly
    public int MaxBackupCount { get; set; } = 5;
    public DateTime? LastBackupAt { get; set; }
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 760;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool IsMaximized { get; set; } = false;
    public string SortBy { get; set; } = "Custom";
    public string ViewMode { get; set; } = "Grid"; // Grid, List
    public string LaunchClickMode { get; set; } = "DoubleClick"; // DoubleClick, SingleClick
    public bool AutoRelocateMissing { get; set; } = true;
}
