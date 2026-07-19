using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.PatientDocuments.Infrastructure;

namespace WebApi.Features.PatientDocuments;

public class PatientDocumentsService(
    IPatientDocumentsRepository repository,
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

    public async Task<Result<IEnumerable<PatientDocumentItemResponse>>> GetListAsync(
        Guid patientId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<PatientDocumentItemResponse>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetListAsync(GetTenantId(), patientId, ct);
        return Result<IEnumerable<PatientDocumentItemResponse>>.Ok(rows.Select(MapItem));
    }

    public async Task<Result<PatientDocumentDownload>> DownloadAsync(
        Guid patientId, Guid documentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PatientDocumentDownload>();
        if (tenantError != null) return tenantError;

        var tenantId = GetTenantId();
        var row = await repository.GetByIdAsync(tenantId, patientId, documentId, ct);
        if (row == null)
            return Result<PatientDocumentDownload>.Fail(ErrorCode.NotFound, "Document not found.");

        var absolutePath = ResolveAbsolutePath(tenantId, patientId, row.StoredFileName);
        if (!File.Exists(absolutePath))
            return Result<PatientDocumentDownload>.Fail(ErrorCode.NotFound, "File content not found on server.");

        var bytes = await File.ReadAllBytesAsync(absolutePath, ct);
        return Result<PatientDocumentDownload>.Ok(new PatientDocumentDownload(
            row.DisplayName,
            row.StoredFileName,
            GuessContentType(row.StoredFileName),
            bytes));
    }

    public async Task<Result<PatientDocumentItemResponse>> UploadAsync(
        Guid patientId, IFormFile file, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PatientDocumentItemResponse>();
        if (tenantError != null) return tenantError;

        if (file == null || file.Length == 0)
            return Result<PatientDocumentItemResponse>.Fail(ErrorCode.Validation, "File is required.");

        if (file.Length > MaxFileBytes)
            return Result<PatientDocumentItemResponse>.Fail(ErrorCode.Validation, "File must be 10 MB or smaller.");

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName))
            return Result<PatientDocumentItemResponse>.Fail(ErrorCode.Validation, "File name is required.");

        if (originalName.Length > 260)
            return Result<PatientDocumentItemResponse>.Fail(ErrorCode.Validation, "File name is too long.");

        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return Result<PatientDocumentItemResponse>.Fail(
                ErrorCode.Validation,
                "Allowed types: PDF, images (PNG, JPG, WebP, GIF), or Word (DOC, DOCX).");

        var tenantId = GetTenantId();
        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absoluteDir = GetPatientDirectory(tenantId, patientId);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, storedFileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream, ct);

        try
        {
            var row = await repository.SaveAsync(
                tenantId,
                patientId,
                originalName.Trim(),
                storedFileName,
                GetUserId(),
                ct);
            return Result<PatientDocumentItemResponse>.Ok(MapItem(row), "Document uploaded.");
        }
        catch
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
            throw;
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid patientId, Guid documentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = GetTenantId();
        var row = await repository.GetByIdAsync(tenantId, patientId, documentId, ct);
        if (row == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Document not found.");

        await repository.DeleteAsync(tenantId, patientId, documentId, GetUserId(), ct);

        var absolutePath = ResolveAbsolutePath(tenantId, patientId, row.StoredFileName);
        if (File.Exists(absolutePath))
        {
            try { File.Delete(absolutePath); }
            catch { /* soft-deleted in DB; disk cleanup best-effort */ }
        }

        return Result<bool>.Ok(true, "Document deleted.");
    }

    private string GetPatientDirectory(Guid tenantId, Guid patientId)
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
            "patients",
            patientId.ToString("N"));
    }

    private string ResolveAbsolutePath(Guid tenantId, Guid patientId, string storedFileName)
    {
        var fileName = Path.GetFileName(storedFileName);
        var root = Path.GetFullPath(GetPatientDirectory(tenantId, patientId));
        var absolutePath = Path.GetFullPath(Path.Combine(root, fileName));
        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid file path.");
        return absolutePath;
    }

    private static PatientDocumentItemResponse MapItem(PatientDocumentRow row) => new(
        row.PatientDocumentId,
        row.PatientId,
        row.DisplayName,
        row.StoredFileName,
        FileTypeLabel(row.StoredFileName),
        row.CreatedAt);

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
