using PortableHub.Core.Models;

namespace PortableHub.Core.Interfaces;

public interface ILaunchService
{
    Task<LaunchResult> LaunchAsync(Software software);
    bool IsRunning(Software software);
    bool ActivateExistingWindow(Software software);
}
