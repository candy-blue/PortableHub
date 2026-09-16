using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PortableHub.App.ViewModels;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class UiUxProMaxTests
{
    [Fact]
    public async Task MainViewModel_ClearSearchCommand_ResetsSearchTextAndUpdatesState()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        var searchService = new SearchService();
        var launchService = new LaunchService(env.SoftwareRepository);
        var scannerService = new FileScannerService();
        var iconService = new IconService(env.SettingsService);
        var pathRepairService = new PathRepairService(env.SoftwareRepository);

        var vm = new MainViewModel(
            env.SoftwareRepository, env.CategoryRepository, env.RootRepository, launchService,
            iconService, searchService, scannerService, pathRepairService,
            env.SettingsService
        );

        await vm.InitializeAsync();

        vm.SearchText = "nonexistent_app_query";
        Assert.True(vm.IsSearchActive);
        Assert.True(vm.IsSearchEmptyState);
        Assert.False(vm.IsEmptyLibraryState);

        // Execute ClearSearchCommand
        Assert.True(vm.ClearSearchCommand.CanExecute(null));
        vm.ClearSearchCommand.Execute(null);

        Assert.Equal(string.Empty, vm.SearchText);
        Assert.False(vm.IsSearchActive);
        Assert.False(vm.IsSearchEmptyState);
        // With an empty database, clearing search should restore IsEmptyLibraryState
        Assert.True(vm.IsEmptyLibraryState);
    }

    [Fact]
    public async Task MainViewModel_EmptyStates_DistinguishEmptyLibraryFromEmptySearchResults()
    {
        using var env = new TestEnvironment();
        await env.InitializeAsync();

        // Add one software to DB
        var app = new Software
        {
            Name = "Calculator Portable",
            ExePath = @"C:\Tools\calc.exe",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow
        };
        await env.SoftwareRepository.AddAsync(app);

        var searchService = new SearchService();
        var launchService = new LaunchService(env.SoftwareRepository);
        var scannerService = new FileScannerService();
        var iconService = new IconService(env.SettingsService);
        var pathRepairService = new PathRepairService(env.SoftwareRepository);

        var vm = new MainViewModel(
            env.SoftwareRepository, env.CategoryRepository, env.RootRepository, launchService,
            iconService, searchService, scannerService, pathRepairService,
            env.SettingsService
        );

        await vm.InitializeAsync();

        // 1. Initially, software exists, neither empty state should be visible
        Assert.Single(vm.FilteredSoftware);
        Assert.False(vm.IsEmptyLibraryState);
        Assert.False(vm.IsSearchEmptyState);

        // 2. Search for something non-existent
        vm.SearchText = "NotExistingKey123";
        Assert.Empty(vm.FilteredSoftware);
        Assert.True(vm.IsSearchActive);
        Assert.True(vm.IsSearchEmptyState);
        Assert.False(vm.IsEmptyLibraryState);

        // 3. Clear search text -> library software restored
        vm.SearchText = string.Empty;
        Assert.Single(vm.FilteredSoftware);
        Assert.False(vm.IsSearchActive);
        Assert.False(vm.IsSearchEmptyState);
        Assert.False(vm.IsEmptyLibraryState);
    }

    [Fact]
    public void QuickLauncherViewModel_HasNoResults_TrueWhenQueryYieldsZeroResults()
    {
        using var env = new TestEnvironment();
        var searchService = new SearchService();
        var launchService = new LaunchService(env.SoftwareRepository);

        var vm = new QuickLauncherViewModel(searchService, launchService);
        vm.OnOpened();

        // Empty query initially
        Assert.False(vm.HasNoResults);

        // Query with no match
        vm.SearchQuery = "RandomUnmatchedString_9999";
        Assert.Empty(vm.Results);
        Assert.True(vm.HasNoResults);

        // Reset query
        vm.SearchQuery = string.Empty;
        Assert.False(vm.HasNoResults);
    }

    [Fact]
    public void XamlFiles_VerifyKeyElementsPresent()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "PortableHub.sln")))
        {
            current = current.Parent;
        }
        Assert.NotNull(current);

        var appDir = Path.Combine(current.FullName, "PortableHub.App");

        // MainWindow.xaml checks
        var mainXaml = File.ReadAllText(Path.Combine(appDir, "Views", "MainWindow.xaml"));
        Assert.Contains("ClearSearchCommand", mainXaml);
        Assert.Contains("IsEmptyLibraryState", mainXaml);
        Assert.Contains("IsSearchEmptyState", mainXaml);
        Assert.Contains("IsSearchActive", mainXaml);
        Assert.Contains("Loc_Sort_RecentLaunch", mainXaml);
        Assert.Contains("Loc_Sort_LaunchCount", mainXaml);

        // QuickLauncherWindow.xaml checks
        var quickXaml = File.ReadAllText(Path.Combine(appDir, "Views", "QuickLauncherWindow.xaml"));
        Assert.Contains("HasNoResults", quickXaml);
        Assert.Contains("Loc_QuickLaunch_NoResult", quickXaml);
    }
}
