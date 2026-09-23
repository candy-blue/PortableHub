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
    public void GithubUpdateService_StripHtml_RemovesTagsAndDecodesEntities()
    {
        string html = "<h3>Portable Hub &amp; Tool</h3><p>Line 1</p><br/><ul><li>Feature &lt;A&gt;</li></ul>";
        string result = GithubUpdateService.StripHtml(html);
        Assert.DoesNotContain("<h3>", result);
        Assert.DoesNotContain("<li>", result);
        Assert.DoesNotContain("&amp;", result);
        Assert.Contains("Portable Hub & Tool", result);
        Assert.Contains("Feature <A>", result);
    }

    [Fact]
    public void GithubUpdateService_ParseAtomFeed_ExtractsLatestReleaseCorrectly()
    {
        string atomXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<feed xmlns=""http://www.w3.org/2005/Atom"">
  <entry>
    <id>tag:github.com,2008:Repository/123/v1.1.0</id>
    <updated>2026-09-18T15:32:56Z</updated>
    <link rel=""alternate"" type=""text/html"" href=""https://github.com/candy-blue/PortableHub/releases/tag/v1.1.0""/>
    <title>Portable Hub v1.1.0</title>
    <content type=""html"">&lt;p&gt;Bug fixes &amp;amp; enhancements&lt;/p&gt;</content>
  </entry>
</feed>";

        var currentVer = new Version(1, 0, 0);
        var updateInfo = GithubUpdateService.ParseAtomFeed(atomXml, currentVer, "v1.0.0");

        Assert.NotNull(updateInfo);
        Assert.True(updateInfo.HasUpdate);
        Assert.Equal("v1.1.0", updateInfo.LatestVersion);
        Assert.Equal("Portable Hub v1.1.0", updateInfo.ReleaseTitle);
        Assert.Equal("https://github.com/candy-blue/PortableHub/releases/tag/v1.1.0", updateInfo.ReleaseUrl);
        Assert.Contains("Bug fixes & enhancements", updateInfo.ReleaseNotes);
        Assert.NotNull(updateInfo.PublishedAt);
    }

    private class MockHttpMessageHandler : System.Net.Http.HttpMessageHandler
    {
        private readonly Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> _handler;
        public MockHttpMessageHandler(Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, 
            System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task GithubUpdateService_CheckForUpdatesAsync_FallsBackToAtomWhenRestApiReturns403()
    {
        GithubUpdateService.ResetRateLimitCache();

        string atomXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<feed xmlns=""http://www.w3.org/2005/Atom"">
  <entry>
    <id>tag:github.com,2008:Repository/123/v1.1.0</id>
    <updated>2026-09-18T15:32:56Z</updated>
    <link rel=""alternate"" type=""text/html"" href=""https://github.com/candy-blue/PortableHub/releases/tag/v1.1.0""/>
    <title>Portable Hub v1.1.0</title>
    <content type=""html"">Update notes</content>
  </entry>
</feed>";

        int restApiCallCount = 0;
        int atomCallCount = 0;

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.ToString().Contains("api.github.com"))
            {
                restApiCallCount++;
                var resp = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    ReasonPhrase = "rate limit exceeded"
                };
                resp.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(40).ToUnixTimeSeconds().ToString());
                return resp;
            }
            if (req.RequestUri.ToString().Contains("releases.atom"))
            {
                atomCallCount++;
                var resp = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.StringContent(atomXml, System.Text.Encoding.UTF8, "application/atom+xml")
                };
                return resp;
            }
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var client = new System.Net.Http.HttpClient(handler);
        var service = new GithubUpdateService(client);

        var result = await service.CheckForUpdatesAsync();

        Assert.NotNull(result);
        Assert.Equal("v1.1.0", result.LatestVersion);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, restApiCallCount);
        Assert.Equal(1, atomCallCount);
        Assert.NotNull(GithubUpdateService.ApiRateLimitedUntil);

        // Second call should skip REST API because of cooldown
        var result2 = await service.CheckForUpdatesAsync();
        Assert.NotNull(result2);
        Assert.Equal(1, restApiCallCount); // REST API was NOT called again
        Assert.Equal(2, atomCallCount);
    }

    [Fact]
    public async Task GithubUpdateService_Live_CanCheckForUpdatesWithoutThrowing()
    {
        var service = new GithubUpdateService();
        var result = await service.CheckForUpdatesAsync();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.LatestVersion));
        // ErrorMessage should be null since Atom fallback succeeds even if REST API is 403
        Assert.Null(result.ErrorMessage);
        Assert.True(result.HasUpdate);
        Assert.Equal("v1.1.0", result.LatestVersion);
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
