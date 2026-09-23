using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class GithubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private const string RepoOwner = "candy-blue";
    private const string RepoName = "PortableHub";
    public const string ReleasesApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
    public const string ReleasesAtomUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases.atom";
    public const string ReleasesLatestWebUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest";
    public const string ReleasesFallbackWebUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases";

    private static DateTimeOffset? _apiRateLimitedUntil;
    private readonly Version? _currentVersionOverride;

    public GithubUpdateService(HttpClient? httpClient = null, Version? currentVersionOverride = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _currentVersionOverride = currentVersionOverride;
    }

    public static void ResetRateLimitCache()
    {
        _apiRateLimitedUntil = null;
    }

    public static DateTimeOffset? ApiRateLimitedUntil => _apiRateLimitedUntil;

    public Version CurrentVersion => _currentVersionOverride ?? GetCurrentVersion();

    public static Version GetCurrentVersion()
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry != null && entry.GetName().Name?.StartsWith("PortableHub", StringComparison.OrdinalIgnoreCase) == true)
        {
            var v = entry.GetName().Version;
            if (v != null) return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
        }

        var asm = typeof(GithubUpdateService).Assembly;
        var ver = asm.GetName().Version;
        return ver != null ? new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build)) : new Version(1, 0, 0);
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVer = CurrentVersion;
        var currentVerStr = $"v{currentVer.Major}.{currentVer.Minor}.{currentVer.Build}";

        bool isRateLimited = _apiRateLimitedUntil.HasValue && DateTimeOffset.UtcNow < _apiRateLimitedUntil.Value;
        string? lastError = null;

        // 1. Try official REST API unless currently in rate limit cooldown
        if (!isRateLimited)
        {
            var (apiInfo, rateLimited, error) = await TryFetchFromRestApiAsync(currentVer, currentVerStr, cancellationToken);
            if (apiInfo != null)
            {
                return apiInfo;
            }
            if (rateLimited)
            {
                isRateLimited = true;
            }
            lastError = error;
        }

        // 2. Fallback to GitHub Releases Atom Feed (Public, no authentication required, not subject to REST API 60 req/hr rate limit)
        var atomInfo = await TryFetchFromAtomFeedAsync(currentVer, currentVerStr, cancellationToken);
        if (atomInfo != null)
        {
            return atomInfo;
        }

        // 3. Fallback to GitHub Releases Web redirect (https://github.com/{owner}/{repo}/releases/latest -> 302 /tag/{tag})
        var webInfo = await TryFetchFromWebRedirectAsync(currentVer, currentVerStr, cancellationToken);
        if (webInfo != null)
        {
            return webInfo;
        }

        // 4. All channels failed - provide user-friendly message
        string errorMsg = isRateLimited
            ? "GitHub API 访问受限 (Rate Limit Exceeded)，且备用更新通道暂不可用，请访问发布页面手动获取最新版本。"
            : (!string.IsNullOrWhiteSpace(lastError) ? lastError : "无法连接到 GitHub 检查更新，请访问发布页面手动获取最新版本。");

        return new UpdateInfo(
            CurrentVersion: currentVerStr,
            LatestVersion: currentVerStr,
            HasUpdate: false,
            ReleaseTitle: null,
            ReleaseNotes: null,
            ReleaseUrl: ReleasesFallbackWebUrl,
            PublishedAt: null,
            ErrorMessage: errorMsg
        );
    }

    private async Task<(UpdateInfo? Info, bool RateLimited, string? Error)> TryFetchFromRestApiAsync(
        Version currentVer,
        string currentVerStr,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PortableHub-Updater", currentVer.ToString()));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
            {
                // Rate limit exceeded
                if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
                    long.TryParse(resetValues.FirstOrDefault(), out var resetUnix))
                {
                    _apiRateLimitedUntil = DateTimeOffset.FromUnixTimeSeconds(resetUnix);
                }
                else
                {
                    _apiRateLimitedUntil = DateTimeOffset.UtcNow.AddMinutes(30);
                }
                return (null, true, $"GitHub API 返回状态码: {(int)response.StatusCode} ({response.ReasonPhrase})");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (null, false, $"GitHub API 返回状态码: {(int)response.StatusCode} ({response.ReasonPhrase})");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            string name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : tag;
            string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            string htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? "" : ReleasesFallbackWebUrl;
            DateTime? publishedAt = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt))
            {
                publishedAt = dt;
            }

            var remoteVer = ParseVersion(tag);
            bool hasUpdate = remoteVer != null && remoteVer > currentVer;

            var info = new UpdateInfo(
                CurrentVersion: currentVerStr,
                LatestVersion: string.IsNullOrWhiteSpace(tag) ? currentVerStr : (tag.StartsWith('v') || tag.StartsWith('V') ? tag : "v" + tag),
                HasUpdate: hasUpdate,
                ReleaseTitle: name,
                ReleaseNotes: body,
                ReleaseUrl: htmlUrl,
                PublishedAt: publishedAt
            );

            return (info, false, null);
        }
        catch (Exception ex)
        {
            return (null, false, $"GitHub API 请求失败: {ex.Message}");
        }
    }

    private async Task<UpdateInfo?> TryFetchFromAtomFeedAsync(
        Version currentVer,
        string currentVerStr,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesAtomUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PortableHub-Updater", currentVer.ToString()));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atom+xml"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseAtomFeed(xml, currentVer, currentVerStr);
        }
        catch
        {
            return null;
        }
    }

    private async Task<UpdateInfo?> TryFetchFromWebRedirectAsync(
        Version currentVer,
        string currentVerStr,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesLatestWebUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PortableHub-Updater", currentVer.ToString()));

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            string? targetUrl = response.RequestMessage?.RequestUri?.ToString();
            if (response.Headers.Location != null)
            {
                targetUrl = response.Headers.Location.ToString();
            }

            if (!string.IsNullOrEmpty(targetUrl) && targetUrl.Contains("/releases/tag/"))
            {
                string tag = targetUrl.Substring(targetUrl.LastIndexOf("/tag/", StringComparison.Ordinal) + 5).Trim();
                var remoteVer = ParseVersion(tag);
                bool hasUpdate = remoteVer != null && remoteVer > currentVer;

                return new UpdateInfo(
                    CurrentVersion: currentVerStr,
                    LatestVersion: string.IsNullOrWhiteSpace(tag) ? currentVerStr : (tag.StartsWith('v') || tag.StartsWith('V') ? tag : "v" + tag),
                    HasUpdate: hasUpdate,
                    ReleaseTitle: $"Portable Hub {tag}",
                    ReleaseNotes: null,
                    ReleaseUrl: targetUrl,
                    PublishedAt: null
                );
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static UpdateInfo? ParseAtomFeed(string xml, Version currentVer, string currentVerStr)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace atom = "http://www.w3.org/2005/Atom";
            var entry = doc.Root?.Element(atom + "entry");
            if (entry == null) return null;

            string title = entry.Element(atom + "title")?.Value?.Trim() ?? "";

            string releaseUrl = entry.Elements(atom + "link")
                .FirstOrDefault(l => (string?)l.Attribute("rel") == "alternate" || l.Attribute("rel") == null)?
                .Attribute("href")?.Value?.Trim() ?? ReleasesFallbackWebUrl;

            string tag = "";
            if (!string.IsNullOrEmpty(releaseUrl) && releaseUrl.Contains("/releases/tag/"))
            {
                tag = releaseUrl.Substring(releaseUrl.LastIndexOf("/tag/", StringComparison.Ordinal) + 5).Trim();
            }
            if (string.IsNullOrEmpty(tag))
            {
                var id = entry.Element(atom + "id")?.Value?.Trim() ?? "";
                if (id.Contains('/'))
                {
                    tag = id.Substring(id.LastIndexOf('/') + 1).Trim();
                }
            }

            DateTime? publishedAt = null;
            var updatedStr = entry.Element(atom + "updated")?.Value;
            if (!string.IsNullOrEmpty(updatedStr) && DateTime.TryParse(updatedStr, out var dt))
            {
                publishedAt = dt;
            }

            string rawContent = entry.Element(atom + "content")?.Value ?? "";
            string notes = StripHtml(rawContent);

            var remoteVer = ParseVersion(tag);
            bool hasUpdate = remoteVer != null && remoteVer > currentVer;

            return new UpdateInfo(
                CurrentVersion: currentVerStr,
                LatestVersion: string.IsNullOrWhiteSpace(tag) ? currentVerStr : (tag.StartsWith('v') || tag.StartsWith('V') ? tag : "v" + tag),
                HasUpdate: hasUpdate,
                ReleaseTitle: string.IsNullOrWhiteSpace(title) ? tag : title,
                ReleaseNotes: notes,
                ReleaseUrl: string.IsNullOrWhiteSpace(releaseUrl) ? ReleasesFallbackWebUrl : releaseUrl,
                PublishedAt: publishedAt
            );
        }
        catch
        {
            return null;
        }
    }

    public static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        string text = Regex.Replace(html, @"<(br|p|/p|h\d|/h\d|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "", RegexOptions.None);
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
        return text;
    }

    public static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        string clean = tag.Trim();
        if (clean.StartsWith('v') || clean.StartsWith('V'))
        {
            clean = clean.Substring(1).Trim();
        }

        // Handle tags like 1.2.3-beta or 1.2
        int dashIdx = clean.IndexOf('-');
        if (dashIdx > 0)
        {
            clean = clean.Substring(0, dashIdx);
        }

        var parts = clean.Split('.');
        if (parts.Length == 1 && int.TryParse(parts[0], out int major))
        {
            return new Version(major, 0, 0);
        }
        if (parts.Length == 2 && int.TryParse(parts[0], out major) && int.TryParse(parts[1], out int minor))
        {
            return new Version(major, minor, 0);
        }
        if (Version.TryParse(clean, out var ver))
        {
            return ver;
        }

        return null;
    }
}
