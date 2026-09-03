using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class SearchServiceTests
{
    [Fact]
    public void Search_ShouldMatchName_Category_Tags_And_Path()
    {
        var searchService = new SearchService();

        var softwareList = new List<Software>
        {
            new() { Id = 1, Name = "Everything", ExePath = @"D:\Tools\Everything\Everything.exe", CategoryName = "系统工具", Tags = "#搜索 #文件" },
            new() { Id = 2, Name = "Visual Studio Code", ExePath = @"D:\Dev\VSCode\Code.exe", CategoryName = "开发工具", Tags = "#代码 #编辑器" },
            new() { Id = 3, Name = "Photoshop Portable", ExePath = @"D:\Apps\Photoshop.exe", CategoryName = "图形图像", Tags = "#修图 #设计" },
            new() { Id = 4, Name = "PhotoScape", ExePath = @"D:\Apps\PhotoScape.exe", CategoryName = "图形图像", Tags = "#拼图" }
        };

        searchService.IndexSoftware(softwareList);

        // 1. Match by name
        var resultName = searchService.Search("every");
        Assert.Single(resultName);
        Assert.Equal("Everything", resultName[0].Name);

        // 2. Match by tag
        var resultTag = searchService.Search("编辑器");
        Assert.Single(resultTag);
        Assert.Equal("Visual Studio Code", resultTag[0].Name);

        // 3. Match by category
        var resultCat = searchService.Search("开发工具");
        Assert.Single(resultCat);
        Assert.Equal("Visual Studio Code", resultCat[0].Name);

        // 4. Match photo prefix returns both photo apps
        var resultPhoto = searchService.Search("photo");
        Assert.Equal(2, resultPhoto.Count);
    }

    [Fact]
    public void SearchQuickLauncher_ShouldRankExactMatchAndPrefixHigher()
    {
        var searchService = new SearchService();

        var softwareList = new List<Software>
        {
            new() { Id = 1, Name = "Git GUI", ExePath = "git-gui.exe", CategoryName = "开发工具", LaunchCount = 10 },
            new() { Id = 2, Name = "Git", ExePath = "git.exe", CategoryName = "开发工具", LaunchCount = 5 },
            new() { Id = 3, Name = "SmartGit", ExePath = "smartgit.exe", CategoryName = "开发工具", LaunchCount = 50 }
        };

        searchService.IndexSoftware(softwareList);

        var results = searchService.SearchQuickLauncher("git");

        // Exact match "Git" should rank highest, even if "SmartGit" has higher launch count!
        Assert.NotEmpty(results);
        Assert.Equal("Git", results[0].Name);
        Assert.Equal("Git GUI", results[1].Name);
    }
}
