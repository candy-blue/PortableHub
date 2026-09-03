using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class PathRepairServiceTests
{
    [Fact]
    public void ComputeRelativePath_ShouldExtractRelativeSubpath()
    {
        var repairService = new PathRepairService(null!);

        var root = @"D:\PortableApps";
        var full = @"D:\PortableApps\Tools\Everything\Everything.exe";

        var relative = repairService.ComputeRelativePath(full, root);
        Assert.Equal(@"Tools\Everything\Everything.exe", relative);
    }

    [Fact]
    public async Task BatchRelocateRoot_ShouldUpdateMatchingSoftwarePaths()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var id1 = await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "Everything",
            ExePath = @"D:\Portable\Everything\Everything.exe",
            CategoryId = categories[0].Id
        });

        var id2 = await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "Notepad++",
            ExePath = @"D:\Portable\Notepad++\notepad++.exe",
            CategoryId = categories[0].Id
        });

        var id3 = await env.SoftwareRepository.AddAsync(new Software
        {
            Name = "Other App",
            ExePath = @"E:\Tools\Other.exe",
            CategoryId = categories[0].Id
        });

        var repairService = new PathRepairService(env.SoftwareRepository);
        var all = await env.SoftwareRepository.GetAllAsync();

        var relocatedCount = await repairService.BatchRelocateRootAsync(@"D:\Portable", @"F:\MyPortable", all);
        Assert.Equal(2, relocatedCount);

        var updated1 = await env.SoftwareRepository.GetByIdAsync(id1);
        Assert.Equal(@"F:\MyPortable\Everything\Everything.exe", updated1!.ExePath);

        var updated3 = await env.SoftwareRepository.GetByIdAsync(id3);
        Assert.Equal(@"E:\Tools\Other.exe", updated3!.ExePath);
    }
}
