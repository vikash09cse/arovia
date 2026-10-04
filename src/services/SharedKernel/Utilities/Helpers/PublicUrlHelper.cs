using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SharedKernel.Settings;

namespace SharedKernel.Utilities.Helpers;

public class PublicUrlHelper(
    IOptions<AppSettings> appSettings,
    IHttpContextAccessor httpContextAccessor)
{
    public string? ToPublicUrl(string? relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return null;

        var value = relativeOrAbsolute.Trim();
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return value;

        var uploadsRelative = ExtractUploadsRelativePath(value);
        if (uploadsRelative != null)
            return CombineBase(GetPublicBase(), uploadsRelative);

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return value;

        var path = value.StartsWith('/') ? value : "/" + value;
        return CombineBase(GetPublicBase(), path);
    }

    /// <summary>
    /// Extracts a web-root relative path (uploads/...) suitable for Path.Combine(WebRootPath, ...).
    /// Strips any virtual-directory prefix such as /WebAPI before /uploads/.
    /// </summary>
    public string? ToWebRootRelativePath(string? urlOrPath)
    {
        var uploads = ExtractUploadsRelativePath(urlOrPath);
        if (uploads == null) return null;
        return uploads.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
    }

    public static string? ExtractUploadsRelativePath(string? urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath)) return null;

        var value = urlOrPath.Trim().Replace('\\', '/');
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try { value = new Uri(value).AbsolutePath; }
            catch { return null; }
        }

        var idx = value.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            return value[idx..];

        if (value.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return "/" + value;

        return null;
    }

    private string GetPublicBase()
    {
        var configured = appSettings.Value.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;

        var pathBase = request.PathBase.HasValue
            ? request.PathBase.Value!.TrimEnd('/')
            : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}";
    }

    private static string CombineBase(string baseUrl, string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        if (string.IsNullOrEmpty(baseUrl))
            return normalizedPath;
        return baseUrl.TrimEnd('/') + normalizedPath;
    }
}
