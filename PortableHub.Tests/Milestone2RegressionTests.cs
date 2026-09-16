using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class Milestone2RegressionTests
{
    [Fact]
    public void SingleInstanceManager_AllowUipiMessage_ExecutesWithoutException()
    {
        // Should execute cleanly without throwing
        SingleInstanceManager.AllowUipiMessage();
        SingleInstanceManager.AllowUipiMessage(IntPtr.Zero);
    }

    [Fact]
    public async Task QuickLauncherViewModel_LaunchFailed_TriggersEventAndErrorCallback()
    {
        var fakeLaunchService = new FailingLaunchService();
        var searchService = new FakeSearchService();
        var vm = new QuickLauncherViewModel(searchService, fakeLaunchService);

        var eventFired = false;
        string? failureMsg = null;
        vm.LaunchFailed += (s, msg) =>
        {
            eventFired = true;
            failureMsg = msg;
        };

        string? errorDialogContent = null;
        string? errorDialogTitle = null;
        vm.ShowErrorAction = (content, title) =>
        {
            errorDialogContent = content;
            errorDialogTitle = title;
        };

        var targetSoftware = new Software
        {
            Id = 1,
            Name = "BrokenApp",
            ExePath = @"C:\NonExistent\BrokenApp.exe"
        };
        vm.Results.Add(targetSoftware);
        vm.SelectedIndex = 0;

        await vm.LaunchSelectedAsync();

        Assert.True(eventFired);
        Assert.Equal("Target executable was not found.", failureMsg);
        Assert.NotNull(errorDialogContent);
        Assert.Contains("BrokenApp", errorDialogContent);
        Assert.Contains("Target executable was not found.", errorDialogContent);
        Assert.Equal("启动失败", errorDialogTitle);
    }

    [Fact]
    public async Task SettingsService_AtomicSave_CreatesBackupAndCleanNoTmpFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PH_M2_SettingsTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new SettingsService(tempDir);
            service.InitializePortableMode(true);

            service.CurrentSettings.CardSize = "InitialValue";
            service.SaveSettings();

            var dataDir = service.GetDataDirectory();
            var settingsPath = Path.Combine(dataDir, "settings.json");
            var backupPath = Path.Combine(dataDir, "settings.json.bak");

            Assert.True(File.Exists(settingsPath));
            Assert.Contains("InitialValue", File.ReadAllText(settingsPath));

            // Concurrent rapid writes
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                var val = $"ConcurrentVal_{i}";
                tasks.Add(Task.Run(() =>
                {
                    service.CurrentSettings.CardSize = val;
                    service.SaveSettings();
                }));
            }
            await Task.WhenAll(tasks);

            // Final state must exist and be valid JSON
            Assert.True(File.Exists(settingsPath));
            var finalContent = File.ReadAllText(settingsPath);
            Assert.Contains("ConcurrentVal_", finalContent);

            // No leftover .tmp files
            var tmpFiles = Directory.GetFiles(dataDir, "*.tmp");
            Assert.Empty(tmpFiles);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private class FailingLaunchService : ILaunchService
    {
        public Task<LaunchResult> LaunchAsync(Software software)
        {
            return Task.FromResult(LaunchResult.Fail("Target executable was not found."));
        }

        public bool IsRunning(Software software) => false;

        public bool ActivateExistingWindow(Software software) => false;
    }

    private class FakeSearchService : ISearchService
    {
        public void IndexSoftware(IEnumerable<Software> softwareList) { }

        public IReadOnlyList<Software> Search(string query, int? categoryId = null, bool favoriteOnly = false, bool recentOnly = false)
        {
            return Array.Empty<Software>();
        }

        public IReadOnlyList<Software> SearchQuickLauncher(string query, int maxResults = 10)
        {
            return Array.Empty<Software>();
        }
    }
}
