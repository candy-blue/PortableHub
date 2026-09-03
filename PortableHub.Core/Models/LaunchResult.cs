namespace PortableHub.Core.Models;

public class LaunchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsAlreadyRunning { get; set; }
    public bool ActivatedExistingWindow { get; set; }
    public int? ProcessId { get; set; }

    public static LaunchResult Ok(int? processId = null, bool activated = false) =>
        new() { Success = true, ProcessId = processId, ActivatedExistingWindow = activated };

    public static LaunchResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
