using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PortableHub.App.Services;
using Xunit;

namespace PortableHub.Tests;

public class LocalizationParityTests
{
    private static readonly string AppProjectDirectory = GetAppProjectDirectory();

    private static string GetAppProjectDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "PortableHub.sln")))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            throw new DirectoryNotFoundException("Could not locate PortableHub.sln from " + baseDir);
        }

        return Path.Combine(current.FullName, "PortableHub.App");
    }

    private static Dictionary<string, string> ExtractKeysFromXaml(string filePath)
    {
        Assert.True(File.Exists(filePath), $"File not found: {filePath}");
        var content = File.ReadAllText(filePath);
        var matches = Regex.Matches(content, @"x:Key=""([^""]+)""[^>]*>([^<]*)<");
        var result = new Dictionary<string, string>();
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            var val = m.Groups[2].Value;
            result[key] = val;
        }
        return result;
    }

    [Fact]
    public void Localization_AllThreeLocales_HaveIdenticalKeySetsAndNonEmptyValues()
    {
        var locDir = Path.Combine(AppProjectDirectory, "Themes", "Localization");
        var zhCNPath = Path.Combine(locDir, "Strings.zh-CN.xaml");
        var zhTWPath = Path.Combine(locDir, "Strings.zh-TW.xaml");
        var enUSPath = Path.Combine(locDir, "Strings.en-US.xaml");

        var zhCNDict = ExtractKeysFromXaml(zhCNPath);
        var zhTWDict = ExtractKeysFromXaml(zhTWPath);
        var enUSDict = ExtractKeysFromXaml(enUSPath);

        // Expected minimum key count is 193
        Assert.True(zhCNDict.Count >= 193, $"zh-CN should have at least 193 keys, found {zhCNDict.Count}");
        Assert.Equal(zhCNDict.Count, zhTWDict.Count);
        Assert.Equal(zhCNDict.Count, enUSDict.Count);

        // Check key parity
        var cnKeys = new HashSet<string>(zhCNDict.Keys);
        var twKeys = new HashSet<string>(zhTWDict.Keys);
        var enKeys = new HashSet<string>(enUSDict.Keys);

        var missingInTw = cnKeys.Except(twKeys).ToList();
        var missingInEn = cnKeys.Except(enKeys).ToList();
        var extraInTw = twKeys.Except(cnKeys).ToList();
        var extraInEn = enKeys.Except(cnKeys).ToList();

        Assert.Empty(missingInTw);
        Assert.Empty(missingInEn);
        Assert.Empty(extraInTw);
        Assert.Empty(extraInEn);

        // Check non-empty values
        foreach (var (k, v) in zhCNDict)
        {
            Assert.False(string.IsNullOrWhiteSpace(v), $"zh-CN key '{k}' has empty translation");
        }
        foreach (var (k, v) in zhTWDict)
        {
            Assert.False(string.IsNullOrWhiteSpace(v), $"zh-TW key '{k}' has empty translation");
        }
        foreach (var (k, v) in enUSDict)
        {
            Assert.False(string.IsNullOrWhiteSpace(v), $"en-US key '{k}' has empty translation");
        }
    }

    [Fact]
    public void Localization_PlaceholderTokens_MatchAcrossLocales()
    {
        var locDir = Path.Combine(AppProjectDirectory, "Themes", "Localization");
        var zhCNDict = ExtractKeysFromXaml(Path.Combine(locDir, "Strings.zh-CN.xaml"));
        var zhTWDict = ExtractKeysFromXaml(Path.Combine(locDir, "Strings.zh-TW.xaml"));
        var enUSDict = ExtractKeysFromXaml(Path.Combine(locDir, "Strings.en-US.xaml"));

        foreach (var key in zhCNDict.Keys)
        {
            var cnTokens = Regex.Matches(zhCNDict[key], @"\{(\d+)\}").Select(m => m.Value).OrderBy(x => x).ToList();
            var twTokens = Regex.Matches(zhTWDict[key], @"\{(\d+)\}").Select(m => m.Value).OrderBy(x => x).ToList();
            var enTokens = Regex.Matches(enUSDict[key], @"\{(\d+)\}").Select(m => m.Value).OrderBy(x => x).ToList();

            Assert.True(cnTokens.SequenceEqual(twTokens), $"Token mismatch between zh-CN and zh-TW for key '{key}'");
            Assert.True(cnTokens.SequenceEqual(enTokens), $"Token mismatch between zh-CN and en-US for key '{key}'");
        }
    }

    [Fact]
    public void LocalizationService_GetFormattedString_FormatsParametersCorrectly()
    {
        var service = LocalizationService.Instance;
        var formatted = service.GetFormattedString("Loc_Scanner_FoundCountFormat", "Found {0} items", 42);
        Assert.Contains("42", formatted);
    }

    [Fact]
    public void LocalizationService_GetFormattedString_FallsBackWhenFormatFails()
    {
        var service = LocalizationService.Instance;
        var formatted = service.GetFormattedString("NonExistentFormatKey", "Fallback {0}", "val");
        Assert.Equal("Fallback val", formatted);
    }
}
