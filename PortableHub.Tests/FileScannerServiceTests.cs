using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class FileScannerServiceTests
{
    [Theory]
    [InlineData(@"D:\Tools\Everything\uninstall.exe", true)]
    [InlineData(@"D:\Tools\7zip\unins000.exe", true)]
    [InlineData(@"D:\Tools\VSCode\setup.exe", true)]
    [InlineData(@"D:\Tools\Chrome\crashpad_handler.exe", true)]
    [InlineData(@"D:\Tools\App\resources\helper.exe", true)]
    [InlineData(@"D:\Tools\Everything\Everything.exe", false)]
    [InlineData(@"D:\Tools\Notepad++\notepad++.exe", false)]
    [InlineData(@"D:\Tools\WinMerge\WinMergeU.exe", false)]
    public void IsExcludedExe_ShouldFilterAuxiliaryExecutables(string path, bool expectedExcluded)
    {
        var scanner = new FileScannerService();
        var excluded = scanner.IsExcludedExe(path);
        Assert.Equal(expectedExcluded, excluded);
    }

    [Fact]
    public void AnalyzeExe_ShouldDeduceCategoryAndCleanName()
    {
        var scanner = new FileScannerService();
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "开发工具" },
            new() { Id = 2, Name = "系统工具" },
            new() { Id = 3, Name = "网络工具" },
            new() { Id = 4, Name = "多媒体" },
            new() { Id = 5, Name = "其他" }
        };

        var candidate1 = scanner.AnalyzeExe(@"D:\Tools\VLC_Player_x64.exe", categories);
        Assert.Equal("多媒体", candidate1.DeducedCategory);
        Assert.Equal(4, candidate1.CategoryId);

        var candidate2 = scanner.AnalyzeExe(@"D:\Tools\VSCodePortable.exe", categories);
        Assert.Equal("开发工具", candidate2.DeducedCategory);
        Assert.Equal(1, candidate2.CategoryId);

        var candidate3 = scanner.AnalyzeExe(@"D:\Tools\Everything.exe", categories);
        Assert.Equal("系统工具", candidate3.DeducedCategory);
        Assert.Equal(2, candidate3.CategoryId);
    }
}
