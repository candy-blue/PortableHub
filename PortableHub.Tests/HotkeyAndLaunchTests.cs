using PortableHub.Core.Models;
using PortableHub.Infrastructure.Services;
using Xunit;

namespace PortableHub.Tests;

public class HotkeyAndLaunchTests
{
    [Theory]
    [InlineData("Ctrl+Alt+Space", true, 0x20)]
    [InlineData("Ctrl+Shift+F", true, (uint)'F')]
    [InlineData("Alt+Space", true, 0x20)]
    [InlineData("Ctrl+Alt+1", true, (uint)'1')]
    [InlineData("", false, 0)]
    [InlineData("InvalidKeyCombo", false, 0)]
    public void ParseHotkey_ShouldCorrectlyParseModifiersAndKey(string hotkeyString, bool expectedSuccess, uint expectedVk)
    {
        var success = WindowsHotkeyService.ParseHotkey(hotkeyString, out var modifiers, out var vk);
        Assert.Equal(expectedSuccess, success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedVk, vk);
        }
    }

    [Fact]
    public async Task LaunchAsync_ShouldReturnFailure_WhenPathDoesNotExist()
    {
        var launchService = new LaunchService(null!);

        var missingSoftware = new Software
        {
            Name = "Nonexistent App",
            ExePath = @"C:\NonexistentDirectory\MissingExe.exe"
        };

        var result = await launchService.LaunchAsync(missingSoftware);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("文件不存在", result.ErrorMessage);
    }

    [Fact]
    public async Task LaunchAsync_ShouldReturnFailure_WhenPathIsEmpty()
    {
        var launchService = new LaunchService(null!);

        var emptySoftware = new Software
        {
            Name = "Empty App",
            ExePath = ""
        };

        var result = await launchService.LaunchAsync(emptySoftware);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("为空", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Ctrl+Alt+Space", true)]
    [InlineData("Ctrl+Shift+K", true)]
    [InlineData("Alt+F1", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Ctrl+NotAKey", false)]
    [InlineData("JustText", false)]
    public void TestHotkeyAvailable_ShouldValidateSyntaxAndFormat(string hotkey, bool expectedValid)
    {
        using var hotkeyService = new WindowsHotkeyService();
        var available = hotkeyService.TestHotkeyAvailable(hotkey);
        Assert.Equal(expectedValid, available);
    }
}
