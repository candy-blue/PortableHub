using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class LinkedLaunchTests
{
    private static string CreateDummyExe(TestEnvironment env, string name)
    {
        var dummyPath = Path.Combine(env.TempDirectory, name);
        File.WriteAllText(dummyPath, "MZ");
        return dummyPath;
    }

    [Fact]
    public async Task LaunchAsync_ShouldCascadeLaunchLinkedSoftware()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var catId = categories[0].Id;

        var subExe = CreateDummyExe(env, "SubApp.exe");
        var subApp = new Software
        {
            Name = "Sub App",
            ExePath = subExe,
            CategoryId = catId
        };
        var subId = await env.SoftwareRepository.AddAsync(subApp);

        var mainExe = CreateDummyExe(env, "MainApp.exe");
        var mainApp = new Software
        {
            Name = "Main App",
            ExePath = mainExe,
            CategoryId = catId,
            LinkedSoftwareIds = subId.ToString()
        };
        var mainId = await env.SoftwareRepository.AddAsync(mainApp);
        mainApp.Id = mainId;

        // Mock launcher prevents spawning external OS processes during tests
        var launchService = new LaunchService(env.SoftwareRepository, psi => null);
        var result = await launchService.LaunchAsync(mainApp);

        Assert.True(result.Success);

        var retrievedMain = await env.SoftwareRepository.GetByIdAsync(mainId);
        var retrievedSub = await env.SoftwareRepository.GetByIdAsync(subId);

        Assert.Equal(1, retrievedMain!.LaunchCount);
        Assert.Equal(1, retrievedSub!.LaunchCount);
    }

    [Fact]
    public async Task LaunchAsync_ShouldPreventInfiniteRecursion_WhenCyclesExist()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var catId = categories[0].Id;

        var exeA = CreateDummyExe(env, "AppA.exe");
        var exeB = CreateDummyExe(env, "AppB.exe");

        // App A links to App B, and App B links to App A
        var appA = new Software
        {
            Name = "App A",
            ExePath = exeA,
            CategoryId = catId
        };
        var idA = await env.SoftwareRepository.AddAsync(appA);

        var appB = new Software
        {
            Name = "App B",
            ExePath = exeB,
            CategoryId = catId,
            LinkedSoftwareIds = idA.ToString()
        };
        var idB = await env.SoftwareRepository.AddAsync(appB);

        // Update appA to link to appB
        appA.Id = idA;
        appA.LinkedSoftwareIds = idB.ToString();
        await env.SoftwareRepository.UpdateAsync(appA);

        var launchService = new LaunchService(env.SoftwareRepository, psi => null);
        var result = await launchService.LaunchAsync(appA);

        Assert.True(result.Success);

        var retrievedA = await env.SoftwareRepository.GetByIdAsync(idA);
        var retrievedB = await env.SoftwareRepository.GetByIdAsync(idB);

        // Both should be launched exactly once without infinite recursion
        Assert.Equal(1, retrievedA!.LaunchCount);
        Assert.Equal(1, retrievedB!.LaunchCount);
    }

    [Fact]
    public async Task LaunchAsync_ShouldNotExceedMaximumOf5LinkedApps()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var categories = await env.CategoryRepository.GetAllAsync();
        var catId = categories[0].Id;

        var linkedIds = new List<int>();
        for (int i = 1; i <= 7; i++)
        {
            var exe = CreateDummyExe(env, $"LinkedApp{i}.exe");
            var app = new Software
            {
                Name = $"Linked App {i}",
                ExePath = exe,
                CategoryId = catId
            };
            linkedIds.Add(await env.SoftwareRepository.AddAsync(app));
        }

        var mainExe = CreateDummyExe(env, "MainAppWithLinks.exe");
        var mainApp = new Software
        {
            Name = "Main App",
            ExePath = mainExe,
            CategoryId = catId,
            LinkedSoftwareIds = string.Join(",", linkedIds)
        };
        var mainId = await env.SoftwareRepository.AddAsync(mainApp);
        mainApp.Id = mainId;

        var launchService = new LaunchService(env.SoftwareRepository, psi => null);
        var result = await launchService.LaunchAsync(mainApp);

        Assert.True(result.Success);

        int totalLinkedLaunched = 0;
        foreach (var id in linkedIds)
        {
            var app = await env.SoftwareRepository.GetByIdAsync(id);
            if (app!.LaunchCount > 0)
            {
                totalLinkedLaunched += app.LaunchCount;
            }
        }

        // Must strictly not exceed 5 linked apps
        Assert.Equal(5, totalLinkedLaunched);
    }
}
