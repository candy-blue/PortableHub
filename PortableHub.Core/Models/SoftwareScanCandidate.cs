namespace PortableHub.Core.Models;

public class SoftwareScanCandidate
{
    public string ExePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DeducedName { get; set; } = string.Empty;
    public string DeducedCategory { get; set; } = "其他";
    public int CategoryId { get; set; }
    public string? FileDescription { get; set; }
    public string? ProductName { get; set; }
    public string? CompanyName { get; set; }
    public string? FileVersion { get; set; }
    public int ConfidenceScore { get; set; } = 100;
    public bool IsSelected { get; set; } = true;
    public string? RelativePath { get; set; }
    public int? RootId { get; set; }
}
