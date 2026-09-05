using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PortableHub.App.Services;
using PortableHub.App.ViewModels;
using PortableHub.Core.Interfaces;
using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class LocalizationAndUpdateTests
{
    [Fact]
    public void LocalizationService_SupportedLanguages_ContainsExpectedLanguages()
    {
        var service = LocalizationService.Instance;
        Assert.NotNull(service);

        var languages = service.SupportedLanguages.ToList();
        Assert.Contains(languages, l => l.Code == "zh-CN");
        Assert.Contains(languages, l => l.Code == "zh-TW");
        Assert.Contains(languages, l => l.Code == "en-US");
    }

    [Fact]
    public void LocalizationService_LanguageSwitching_TriggersEventAndUpdatesCurrentLanguage()
    {
        var service = LocalizationService.Instance;
        var eventFired = false;

        void OnLanguageChanged(object? sender, EventArgs e)
        {
            eventFired = true;
        }

        service.LanguageChanged += OnLanguageChanged;
        try
        {
            service.SetLanguage("en-US");
            Assert.True(eventFired);
            Assert.Equal("en-US", service.CurrentLanguage);
        }
        finally
        {
            service.LanguageChanged -= OnLanguageChanged;
            service.SetLanguage("zh-CN");
        }
    }

    [Fact]
    public void LocalizationService_GetString_ReturnsFallbackWhenKeyNotFound()
    {
        var service = LocalizationService.Instance;
        var result = service.GetString("NonExistent_Key_XYZ", "DefaultFallback");
        Assert.Equal("DefaultFallback", result);
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("v2.5.0-beta1", 2, 5, 0)]
    [InlineData("v0.10.2.1", 0, 10, 2)]
    public void GithubUpdateService_ParseVersion_ParsesValidSemverStrings(string tag, int major, int minor, int build)
    {
        var parsed = GithubUpdateService.ParseVersion(tag);
        Assert.NotNull(parsed);
        Assert.Equal(major, parsed.Major);
        Assert.Equal(minor, parsed.Minor);
        Assert.Equal(build, parsed.Build);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("release-candidate")]
    public void GithubUpdateService_ParseVersion_ReturnsNullForInvalidStrings(string tag)
    {
        var parsed = GithubUpdateService.ParseVersion(tag);
        Assert.Null(parsed);
    }

    [Fact]
    public void CategoryEditViewModel_IconOptions_PopulatedAndSelectable()
    {
        var vm = new CategoryEditViewModel();
        Assert.NotEmpty(vm.AvailableIconOptions);
        Assert.NotNull(vm.SelectedIconOption);

        // Pick a different icon from the options
        var targetOption = vm.AvailableIconOptions.FirstOrDefault(o => o.Key == "XboxOneConsole");
        Assert.NotNull(targetOption);

        vm.SelectIconOptionCommand.Execute(targetOption);
        Assert.Equal("XboxOneConsole", vm.Icon);
        Assert.Equal(targetOption, vm.SelectedIconOption);
    }

    [Fact]
    public void CategoryEditViewModel_EditExistingCategory_SelectsCorrectIcon()
    {
        var cat = new Category
        {
            Id = 5,
            Name = "开发工具",
            Icon = "Repair",
            Color = "#10B981"
        };

        var vm = new CategoryEditViewModel(cat);
        Assert.Equal("开发工具", vm.Name);
        Assert.Equal("Repair", vm.Icon);
        Assert.Equal("#10B981", vm.Color);
        Assert.NotNull(vm.SelectedIconOption);
        Assert.Equal("Repair", vm.SelectedIconOption.Key);
    }
}
