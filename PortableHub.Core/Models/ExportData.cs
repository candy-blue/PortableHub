namespace PortableHub.Core.Models;

public class ExportData
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<ExportCategoryItem> Categories { get; set; } = [];
    public List<ExportSoftwareItem> Software { get; set; } = [];
    public List<ExportRootItem> Roots { get; set; } = [];
}

public class ExportCategoryItem
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}

public class ExportSoftwareItem
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string ExePath { get; set; } = string.Empty;
    public string? RelativePath { get; set; }
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public bool IsFavorite { get; set; }
    public bool RunAsAdmin { get; set; }
    public bool SingleInstance { get; set; } = true;
}

public class ExportRootItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
