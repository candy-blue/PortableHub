namespace PortableHub.Core.Models;

public class BackupManifest
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string AppVersion { get; set; } = "1.0.0";
    public int SoftwareCount { get; set; }
    public int CategoryCount { get; set; }
    public int RootCount { get; set; }
    public string Description { get; set; } = "PortableHub Backup";
}
