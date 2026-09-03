namespace PortableHub.Core.Models;

public class Software
{
    public int Id { get; set; }
    public int? RootId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string? RelativePath { get; set; }
    public string? Description { get; set; }
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? IconPath { get; set; }
    public int CategoryId { get; set; }
    public bool IsFavorite { get; set; }
    public int LaunchCount { get; set; }
    public DateTime? LastLaunchedAt { get; set; }
    public int SortOrder { get; set; }
    public bool RunAsAdmin { get; set; }
    public bool SingleInstance { get; set; } = true;
    public string? Tags { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Runtime state (not stored directly in DB table)
    public bool IsMissing { get; set; }
    public bool IsRunning { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
