using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using PortableHub.Core.Interfaces;

namespace PortableHub.Infrastructure.Services;

public class GithubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private const string RepoOwner = "candy-blue";
    private const string RepoName = "PortableHub";
    private const string ReleasesApiUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    public GithubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public static Version GetCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(GithubUpdateService).Assembly;
        var ver = asm.GetName().Version;
        return ver != null ? new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build)) : new Version(1, 0, 0);
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVer = GetCurrentVersion();
        var currentVerStr = $"v{currentVer.Major}.{currentVer.Minor}.{currentVer.Build}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PortableHub-Updater", currentVer.ToString()));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateInfo(
                    CurrentVersion: currentVerStr,
                    LatestVersion: currentVerStr,
                    HasUpdate: false,
                    ReleaseTitle: null,
                    ReleaseNotes: null,
                    ReleaseUrl: $"https://github.com/{RepoOwner}/{RepoName}/releases",
                    PublishedAt: null,
                    ErrorMessage: $"GitHub API 返回状态码: {(int)response.StatusCode} ({response.ReasonPhrase})"
                );
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string tag = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            string name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : tag;
            string body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            string htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() ?? "" : $"https://github.com/{RepoOwner}/{RepoName}/releases";
            DateTime? publishedAt = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt))
            {
                publishedAt = dt;
            }

            var remoteVer = ParseVersion(tag);
            bool hasUpdate = remoteVer != null && remoteVer > currentVer;

            return new UpdateInfo(
                CurrentVersion: currentVerStr,
                LatestVersion: string.IsNullOrWhiteSpace(tag) ? currentVerStr : (tag.StartsWith('v') ? tag : "v" + tag),
                HasUpdate: hasUpdate,
                ReleaseTitle: name,
                ReleaseNotes: body,
                ReleaseUrl: htmlUrl,
                PublishedAt: publishedAt
            );
        }
        catch (Exception ex)
        {
            return new UpdateInfo(
                CurrentVersion: currentVerStr,
                LatestVersion: currentVerStr,
                HasUpdate: false,
                ReleaseTitle: null,
                ReleaseNotes: null,
                ReleaseUrl: $"https://github.com/{RepoOwner}/{RepoName}/releases",
                PublishedAt: null,
                ErrorMessage: $"检查更新失败: {ex.Message}"
            );
        }
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
