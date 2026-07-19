using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.CommonFiles.Infrastructure;

namespace WebApi.Features.CommonFiles;

public class CommonFilesService(
    ICommonFilesRepository repository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".doc", ".docx"
    };

    private const long MaxFileBytes = 10 * 1024 * 1024;

    public async Task<Result<IEnumerable<CommonFileItemResponse>>> GetListAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<CommonFileItemResponse>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetListAsync(GetTenantId(), ct);
        return Result<IEnumerable<CommonFileItemResponse>>.Ok(rows.Select(MapItem));
    }

    public async Task<Result<CommonFileDownload>> DownloadAsync(Guid commonFileId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<CommonFileDownload>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetByIdAsync(GetTenantId(), commonFileId, ct);
        if (row == null)
            return Result<CommonFileDownload>.Fail(ErrorCode.NotFound, "File not found.");

        var absolutePath = ResolveAbsolutePath(GetTenantId(), row.StoredFileName);
        if (!File.Exists(absolutePath))
            return Result<CommonFileDownload>.Fail(ErrorCode.NotFound, "File content not found on server.");

        var bytes = await File.ReadAllBytesAsync(absolutePath, ct);
        return Result<CommonFileDownload>.Ok(new CommonFileDownload(
            row.DisplayName,
            row.StoredFileName,
            GuessContentType(row.StoredFileName),
            bytes));
    }

    public async Task<Result<CommonFileItemResponse>> UploadAsync(IFormFile file, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<CommonFileItemResponse>();
        if (tenantError != null) return tenantError;

        if (file == null || file.Length == 0)
            return Result<CommonFileItemResponse>.Fail(ErrorCode.Validation, "File is required.");

        if (file.Length > MaxFileBytes)
            return Result<CommonFileItemResponse>.Fail(ErrorCode.Validation, "File must be 10 MB or smaller.");

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName))
            return Result<CommonFileItemResponse>.Fail(ErrorCode.Validation, "File name is required.");

        if (originalName.Length > 260)
            return Result<CommonFileItemResponse>.Fail(ErrorCode.Validation, "File name is too long.");

        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return Result<CommonFileItemResponse>.Fail(
                ErrorCode.Validation,
                "Allowed types: PDF, images (PNG, JPG, WebP, GIF), or Word (DOC, DOCX).");

        var tenantId = GetTenantId();
        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absoluteDir = GetCommonDirectory(tenantId);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, storedFileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream, ct);

        try
        {
            var row = await repository.SaveAsync(
                tenantId,
                originalName.Trim(),
                storedFileName,
                GetUserId(),
                ct);
            return Result<CommonFileItemResponse>.Ok(MapItem(row), "File uploaded.");
        }
        catch
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
            throw;
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid commonFileId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = GetTenantId();
        var row = await repository.GetByIdAsync(tenantId, commonFileId, ct);
        if (row == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "File not found.");

        await repository.DeleteAsync(tenantId, commonFileId, GetUserId(), ct);

        var absolutePath = ResolveAbsolutePath(tenantId, row.StoredFileName);
        if (File.Exists(absolutePath))
        {
            try { File.Delete(absolutePath); }
            catch { /* soft-deleted in DB; disk cleanup best-effort */ }
        }

        return Result<bool>.Ok(true, "File deleted.");
    }

    private string GetCommonDirectory(Guid tenantId)
    {
        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
        }

        return Path.Combine(webRoot, "uploads", "tenants", tenantId.ToString("N"), "common");
    }

    private string ResolveAbsolutePath(Guid tenantId, string storedFileName)
    {
        var fileName = Path.GetFileName(storedFileName);
        var absolutePath = Path.GetFullPath(Path.Combine(GetCommonDirectory(tenantId), fileName));
        var root = Path.GetFullPath(GetCommonDirectory(tenantId));
        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");
        return absolutePath;
    }

    private static CommonFileItemResponse MapItem(CommonFileRow row) => new(
        row.CommonFileId,
        row.DisplayName,
        row.StoredFileName,
        FileTypeLabel(row.StoredFileName),
        row.CreatedAt);

    private static string FileTypeLabel(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        return string.IsNullOrWhiteSpace(ext) ? "FILE" : ext;
    }

    internal static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;
    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
}
