using System;
using System.Collections.Generic;
using Symbol = iNKORE.UI.WPF.Modern.Controls.Symbol;

namespace PortableHub.App.Helpers;

/// <summary>
/// Resolves icon names, including legacy WPF-UI SymbolRegular names, to iNKORE modern symbols.
/// Guarantees backward compatibility with persisted categories and historical databases (Section 3.1 & 50).
/// </summary>
public static class SymbolHelper
{
    private static readonly Dictionary<string, Symbol> LegacyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Apps"] = Symbol.AllApps,
        ["Apps24"] = Symbol.AllApps,
        ["AllApps"] = Symbol.AllApps,
        ["Settings"] = Symbol.Setting,
        ["Settings24"] = Symbol.Setting,
        ["Setting"] = Symbol.Setting,
        ["Search"] = Symbol.Find,
        ["Search24"] = Symbol.Find,
        ["Find"] = Symbol.Find,
        ["Star"] = Symbol.Favorite,
        ["Star24"] = Symbol.Favorite,
        ["Favorite"] = Symbol.Favorite,
        ["Heart"] = Symbol.Like,
        ["Heart24"] = Symbol.Like,
        ["Like"] = Symbol.Like,
        ["Cloud"] = Symbol.Upload,
        ["Cloud24"] = Symbol.Upload,
        ["Upload"] = Symbol.Upload,
        ["Wrench"] = Symbol.Repair,
        ["Wrench24"] = Symbol.Repair,
        ["Toolbox"] = Symbol.Repair,
        ["Toolbox24"] = Symbol.Repair,
        ["Repair"] = Symbol.Repair,
        ["Code"] = Symbol.Repair,
        ["Code24"] = Symbol.Repair,
        ["Image"] = Symbol.Pictures,
        ["Image24"] = Symbol.Pictures,
        ["Pictures"] = Symbol.Pictures,
        ["Color"] = Symbol.FontColor,
        ["Color24"] = Symbol.FontColor,
        ["FontColor"] = Symbol.FontColor,
        ["Games"] = Symbol.XboxOneConsole,
        ["Games24"] = Symbol.XboxOneConsole,
        ["XboxOneConsole"] = Symbol.XboxOneConsole,
        ["MusicNote2"] = Symbol.Audio,
        ["MusicNote224"] = Symbol.Audio,
        ["Music"] = Symbol.Audio,
        ["Audio"] = Symbol.Audio,
        ["Desktop"] = Symbol.Remote,
        ["Desktop24"] = Symbol.Remote,
        ["Remote"] = Symbol.Remote,
        ["Book"] = Symbol.Read,
        ["Book24"] = Symbol.Read,
        ["Read"] = Symbol.Read,
        ["Chat"] = Symbol.Message,
        ["Chat24"] = Symbol.Message,
        ["Message"] = Symbol.Message,
        ["Rocket"] = Symbol.Send,
        ["Rocket24"] = Symbol.Send,
        ["Send"] = Symbol.Send,
        ["UsbStick"] = Symbol.SaveLocal,
        ["UsbStick24"] = Symbol.SaveLocal,
        ["SaveLocal"] = Symbol.SaveLocal,
        ["ArrowDownload"] = Symbol.Download,
        ["ArrowDownload24"] = Symbol.Download,
        ["Download"] = Symbol.Download,
        ["History"] = Symbol.Clock,
        ["History24"] = Symbol.Clock,
        ["Clock"] = Symbol.Clock,
        ["LockClosed"] = Symbol.Permissions,
        ["LockClosed24"] = Symbol.Permissions,
        ["Shield"] = Symbol.Permissions,
        ["Shield24"] = Symbol.Permissions,
        ["Permissions"] = Symbol.Permissions,
        ["Database"] = Symbol.Library,
        ["Database24"] = Symbol.Library,
        ["Library"] = Symbol.Library,
        ["Folder"] = Symbol.Folder,
        ["Folder24"] = Symbol.Folder,
        ["Play"] = Symbol.Play,
        ["Play24"] = Symbol.Play,
        ["Tag"] = Symbol.Tag,
        ["Tag24"] = Symbol.Tag,
        ["Globe"] = Symbol.Globe,
        ["Globe24"] = Symbol.Globe,
        ["Document"] = Symbol.Document,
        ["Document24"] = Symbol.Document,
        ["Scan"] = Symbol.Scan,
        ["Calculator"] = Symbol.Calculator,
        ["Mail"] = Symbol.Mail,
        ["Link"] = Symbol.Link,
        ["Camera"] = Symbol.Camera,
        ["Edit"] = Symbol.Edit,
        ["Edit24"] = Symbol.Edit,
    };

    public static Symbol ResolveSymbol(string? iconName, Symbol fallback = Symbol.Folder)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return fallback;

        if (LegacyMap.TryGetValue(iconName, out var mapped))
            return mapped;

        var clean = iconName.Replace("24", "");
        if (LegacyMap.TryGetValue(clean, out var cleanMapped))
            return cleanMapped;

        if (Enum.TryParse<Symbol>(clean, true, out var parsedClean))
            return parsedClean;

        if (Enum.TryParse<Symbol>(iconName, true, out var parsed))
            return parsed;

        return fallback;
    }
}
