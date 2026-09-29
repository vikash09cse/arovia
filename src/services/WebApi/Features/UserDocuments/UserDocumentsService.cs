using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.UserDocuments.Infrastructure;
using WebApi.Features.Users.Infrastructure;

namespace WebApi.Features.UserDocuments;

public class UserDocumentsService(
    IUserDocumentsRepository repository,
    IUsersRepository usersRepository,
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

    public async Task<Result<IEnumerable<UserDocumentItemResponse>>> GetListAsync(
        Guid userId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<UserDocumentItemResponse>>();
        if (tenantError != null) return tenantError;

        var access = await EnsureManageableUserAsync(userId, ct);
        if (access != null) return Result<IEnumerable<UserDocumentItemResponse>>.Fail(access.ErrorCode, access.Message);

        var rows = await repository.GetListAsync(GetTenantId(), userId, ct);
        return Result<IEnumerable<UserDocumentItemResponse>>.Ok(rows.Select(MapItem));
    }

    public async Task<Result<UserDocumentDownload>> DownloadAsync(
        Guid userId, Guid documentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<UserDocumentDownload>();
        if (tenantError != null) return tenantError;

        var access = await EnsureManageableUserAsync(userId, ct);
        if (access != null) return Result<UserDocumentDownload>.Fail(access.ErrorCode, access.Message);

        var tenantId = GetTenantId();
        var row = await repository.GetByIdAsync(tenantId, userId, documentId, ct);
        if (row == null)
            return Result<UserDocumentDownload>.Fail(ErrorCode.NotFound, "Document not found.");

        var absolutePath = ResolveAbsolutePath(tenantId, userId, row.StoredFileName);
        if (!File.Exists(absolutePath))
            return Result<UserDocumentDownload>.Fail(ErrorCode.NotFound, "File content not found on server.");

        var bytes = await File.ReadAllBytesAsync(absolutePath, ct);
        return Result<UserDocumentDownload>.Ok(new UserDocumentDownload(
            row.DisplayName,
            row.StoredFileName,
            GuessContentType(row.StoredFileName),
            bytes));
    }

    public async Task<Result<UserDocumentItemResponse>> UploadAsync(
        Guid userId, byte documentType, IFormFile file, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<UserDocumentItemResponse>();
        if (tenantError != null) return tenantError;

        var access = await EnsureManageableUserAsync(userId, ct);
        if (access != null) return Result<UserDocumentItemResponse>.Fail(access.ErrorCode, access.Message);

        if (documentType is < 1 or > 4)
            return Result<UserDocumentItemResponse>.Fail(
                ErrorCode.Validation, "Document type must be Aadhar, Certificate, Pan, or Other.");

        if (file == null || file.Length == 0)
            return Result<UserDocumentItemResponse>.Fail(ErrorCode.Validation, "File is required.");

        if (file.Length > MaxFileBytes)
            return Result<UserDocumentItemResponse>.Fail(ErrorCode.Validation, "File must be 10 MB or smaller.");

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName))
            return Result<UserDocumentItemResponse>.Fail(ErrorCode.Validation, "File name is required.");

        if (originalName.Length > 260)
            return Result<UserDocumentItemResponse>.Fail(ErrorCode.Validation, "File name is too long.");

        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return Result<UserDocumentItemResponse>.Fail(
                ErrorCode.Validation,
                "Allowed types: PDF, images (PNG, JPG, WebP, GIF), or Word (DOC, DOCX).");

        var tenantId = GetTenantId();
        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absoluteDir = GetUserDirectory(tenantId, userId);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, storedFileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream, ct);

        try
        {
            var row = await repository.SaveAsync(
                tenantId,
                userId,
                documentType,
                originalName.Trim(),
                storedFileName,
                GetUserId(),
                ct);
            return Result<UserDocumentItemResponse>.Ok(MapItem(row), "Document uploaded.");
        }
        catch
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
            throw;
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid documentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var access = await EnsureManageableUserAsync(userId, ct);
        if (access != null) return Result<bool>.Fail(access.ErrorCode, access.Message);

        var tenantId = GetTenantId();
        var row = await repository.GetByIdAsync(tenantId, userId, documentId, ct);
        if (row == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Document not found.");

        await repository.DeleteAsync(tenantId, userId, documentId, GetUserId(), ct);

        var absolutePath = ResolveAbsolutePath(tenantId, userId, row.StoredFileName);
        if (File.Exists(absolutePath))
        {
            try { File.Delete(absolutePath); }
            catch { /* soft-deleted in DB; disk cleanup best-effort */ }
        }

        return Result<bool>.Ok(true, "Document deleted.");
    }

    private async Task<Result<object>?> EnsureManageableUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await usersRepository.GetByIdAsync(GetTenantId(), userId, ct);
        if (user == null)
            return Result<object>.Fail(ErrorCode.NotFound, "User not found.");

        if (user.Role is not ((byte)UserType.Staff) and not ((byte)UserType.Doctor))
            return Result<object>.Fail(ErrorCode.Forbidden, "Documents can only be managed for Staff or Doctor accounts.");

        return null;
    }

    private string GetUserDirectory(Guid tenantId, Guid userId)
    {
        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
        }

        return Path.Combine(
            webRoot,
            "uploads",
            "tenants",
            tenantId.ToString("N"),
            "users",
            userId.ToString("N"));
    }

    private string ResolveAbsolutePath(Guid tenantId, Guid userId, string storedFileName)
    {
        var fileName = Path.GetFileName(storedFileName);
        var root = Path.GetFullPath(GetUserDirectory(tenantId, userId));
        var absolutePath = Path.GetFullPath(Path.Combine(root, fileName));
        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");
        return absolutePath;
    }

    private static UserDocumentItemResponse MapItem(UserDocumentRow row) => new(
        row.UserDocumentId,
        row.UserId,
        row.DocumentType,
        DocumentTypeLabel(row.DocumentType),
        row.DisplayName,
        row.StoredFileName,
        FileTypeLabel(row.StoredFileName),
        row.CreatedAt);

    private static string DocumentTypeLabel(byte type) => type switch
    {
        1 => "Aadhar",
        2 => "Certificate",
        3 => "Pan",
        4 => "Other",
        _ => "Unknown"
    };

    private static string FileTypeLabel(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        return string.IsNullOrWhiteSpace(ext) ? "FILE" : ext;
    }

    private static string GuessContentType(string fileName)
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
