using PortableHub.Core.Models;
using Xunit;

namespace PortableHub.Tests;

public class SoftwareRepositoryTests
{
    [Fact]
    public async Task Add_And_GetById_ShouldSupportChineseAndSpecialCharacters()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var category = categories.First(c => c.Name == "其他");

        var testSoftware = new Software
        {
            Name = "代码编辑器 (VS Code 便携版) [测试]",
            ExePath = @"D:\我的便携软件\VS Code (x64)\Code.exe",
            Arguments = "--portable --verbose",
            WorkingDirectory = @"D:\我的便携软件\VS Code (x64)",
            CategoryId = category.Id,
            IsFavorite = true,
            SingleInstance = true,
            Tags = "#开发 #编辑器 #常用",
            SortOrder = 1
        };

        var id = await env.SoftwareRepository.AddAsync(testSoftware);
        Assert.True(id > 0);

        var retrieved = await env.SoftwareRepository.GetByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal("代码编辑器 (VS Code 便携版) [测试]", retrieved.Name);
        Assert.Equal(@"D:\我的便携软件\VS Code (x64)\Code.exe", retrieved.ExePath);
        Assert.Equal("--portable --verbose", retrieved.Arguments);
        Assert.Equal(category.Name, retrieved.CategoryName);
        Assert.True(retrieved.IsFavorite);
        Assert.True(retrieved.SingleInstance);
    }

    [Fact]
    public async Task IncrementLaunchCount_ShouldUpdateCountAndTimestamp()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var software = new Software
        {
            Name = "Everything",
            ExePath = @"D:\Tools\Everything.exe",
            CategoryId = categories[0].Id
        };
        var id = await env.SoftwareRepository.AddAsync(software);

        var before = await env.SoftwareRepository.GetByIdAsync(id);
        Assert.Equal(0, before!.LaunchCount);
        Assert.Null(before.LastLaunchedAt);

        var now = DateTime.UtcNow;
        await env.SoftwareRepository.IncrementLaunchCountAsync(id, now);

        var after = await env.SoftwareRepository.GetByIdAsync(id);
        Assert.Equal(1, after!.LaunchCount);
        Assert.NotNull(after.LastLaunchedAt);
    }

    [Fact]
    public async Task Delete_ShouldRemoveRecord_WithoutDeletingPhysicalFile()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        // Create a fake physical exe file
        var fakeExe = Path.Combine(env.TempDirectory, "FakeApp.exe");
        await File.WriteAllTextAsync(fakeExe, "fake binary content");

        var categories = await env.CategoryRepository.GetAllAsync();
        var software = new Software
        {
            Name = "Fake App",
            ExePath = fakeExe,
            CategoryId = categories[0].Id
        };
        var id = await env.SoftwareRepository.AddAsync(software);

        // Delete from repository
        await env.SoftwareRepository.DeleteAsync(id);

        var retrieved = await env.SoftwareRepository.GetByIdAsync(id);
        Assert.Null(retrieved);

        // CRITICAL PRINCIPLE: Physical file must NOT be deleted!
        Assert.True(File.Exists(fakeExe));
    }

    [Fact]
    public async Task UpdateSortOrders_ShouldUpdateAllSpecifiedSoftware()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var id1 = await env.SoftwareRepository.AddAsync(new Software { Name = "App 1", ExePath = "C:\\app1.exe", CategoryId = categories[0].Id, SortOrder = 1 });
        var id2 = await env.SoftwareRepository.AddAsync(new Software { Name = "App 2", ExePath = "C:\\app2.exe", CategoryId = categories[0].Id, SortOrder = 2 });

        await env.SoftwareRepository.UpdateSortOrdersAsync(new[] { (id1, 10), (id2, 5) });

        var all = await env.SoftwareRepository.GetAllAsync();
        var app1 = all.First(s => s.Id == id1);
        var app2 = all.First(s => s.Id == id2);

        Assert.Equal(10, app1.SortOrder);
        Assert.Equal(5, app2.SortOrder);
    }
}
