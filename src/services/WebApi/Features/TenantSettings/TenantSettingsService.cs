using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.TenantSettings.Infrastructure;

namespace WebApi.Features.TenantSettings;

public class TenantSettingsService(
    ITenantSettingsRepository repository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    PublicUrlHelper publicUrls)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private const long MaxLogoBytes = 2 * 1024 * 1024;

    public async Task<Result<TenantSettingsResponse>> GetAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<TenantSettingsResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetAsync(GetTenantId(), ct);
        if (row == null)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.NotFound, "Tenant not found.");

        return Result<TenantSettingsResponse>.Ok(Map(row));
    }

    public async Task<Result<TenantSettingsResponse>> UpdateAsync(
        UpdateTenantSettingsRequest request, CancellationToken ct)
    {
        var validation = Validate(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<TenantSettingsResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = GetTenantId();
        var existing = await repository.GetAsync(tenantId, ct);
        if (existing == null)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.NotFound, "Tenant not found.");

        var logoUrl = string.IsNullOrWhiteSpace(request.LogoUrl)
            ? existing.LogoUrl
            : PublicUrlHelper.ExtractUploadsRelativePath(request.LogoUrl) ?? request.LogoUrl.Trim();

        var website = string.IsNullOrWhiteSpace(request.Website)
            ? null
            : request.Website.Trim();
        var hospitalRegistrationNo = string.IsNullOrWhiteSpace(request.HospitalRegistrationNo)
            ? null
            : request.HospitalRegistrationNo.Trim();

        await repository.UpdateAsync(
            tenantId,
            request.HospitalName.Trim(),
            request.PrimaryContactFirstName.Trim(),
            request.PrimaryContactLastName.Trim(),
            request.PrimaryContactEmail.Trim(),
            request.PrimaryContactPhone.Trim(),
            request.Address.Trim(),
            request.Timezone.Trim(),
            website,
            hospitalRegistrationNo,
            logoUrl,
            ct);

        var updated = await repository.GetAsync(tenantId, ct);
        return Result<TenantSettingsResponse>.Ok(Map(updated!), "Tenant settings updated.");
    }

    public async Task<Result<TenantSettingsResponse>> UploadLogoAsync(IFormFile file, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<TenantSettingsResponse>();
        if (tenantError != null) return tenantError;

        if (file == null || file.Length == 0)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Logo file is required.");

        if (file.Length > MaxLogoBytes)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Logo must be 2 MB or smaller.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Logo must be a PNG, JPG, or WebP image.");

        var tenantId = GetTenantId();
        var existing = await repository.GetAsync(tenantId, ct);
        if (existing == null)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.NotFound, "Tenant not found.");

        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
        }

        var relativeDir = Path.Combine("uploads", "tenants", tenantId.ToString("N"));
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        // Unique name avoids IIS locking the previous logo.png and busts browser cache.
        var fileName = $"logo_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteDir, fileName);
        await using (var stream = new FileStream(
            absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            await file.CopyToAsync(stream, ct);
        }

        var relativeUrl = $"/uploads/tenants/{tenantId:N}/{fileName}";

        await repository.UpdateAsync(
            tenantId,
            existing.HospitalName,
            existing.PrimaryContactFirstName,
            existing.PrimaryContactLastName,
            existing.PrimaryContactEmail,
            existing.PrimaryContactPhone,
            existing.Address,
            existing.Timezone,
            existing.Website,
            existing.HospitalRegistrationNo,
            relativeUrl,
            ct);

        // Best-effort cleanup of older logos (may be locked while still being served).
        foreach (var old in Directory.EnumerateFiles(absoluteDir, "logo*"))
        {
            if (string.Equals(Path.GetFileName(old), fileName, StringComparison.OrdinalIgnoreCase))
                continue;
            try { File.Delete(old); }
            catch { /* ignore locked/in-use files */ }
        }

        var updated = await repository.GetAsync(tenantId, ct);
        return Result<TenantSettingsResponse>.Ok(Map(updated!), "Logo uploaded.");
    }

    private Result<TenantSettingsResponse>? Validate(UpdateTenantSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.HospitalName))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Hospital name is required.");
        if (request.HospitalName.Trim().Length > 200)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Hospital name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(request.PrimaryContactFirstName))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Contact first name is required.");
        if (string.IsNullOrWhiteSpace(request.PrimaryContactLastName))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Contact last name is required.");
        if (string.IsNullOrWhiteSpace(request.PrimaryContactEmail) || !request.PrimaryContactEmail.Contains('@'))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "A valid contact email is required.");
        if (string.IsNullOrWhiteSpace(request.PrimaryContactPhone))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Contact phone is required.");
        if (request.PrimaryContactPhone.Trim().Length > 15)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Contact phone cannot exceed 15 characters.");
        if (string.IsNullOrWhiteSpace(request.Address))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Address is required.");
        if (string.IsNullOrWhiteSpace(request.Timezone))
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Timezone is required.");
        if (request.Website?.Trim().Length > 200)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Website cannot exceed 200 characters.");
        if (request.HospitalRegistrationNo?.Trim().Length > 100)
            return Result<TenantSettingsResponse>.Fail(ErrorCode.Validation, "Hospital registration number cannot exceed 100 characters.");
        return null;
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;

    private TenantSettingsResponse Map(TenantSettingsRow row) => new(
        row.TenantId,
        row.HospitalName,
        row.Subdomain,
        row.PrimaryContactFirstName,
        row.PrimaryContactLastName,
        row.PrimaryContactEmail,
        row.PrimaryContactPhone,
        row.Address,
        row.Timezone,
        string.IsNullOrWhiteSpace(row.Website) ? null : row.Website.Trim(),
        string.IsNullOrWhiteSpace(row.HospitalRegistrationNo) ? null : row.HospitalRegistrationNo.Trim(),
        string.IsNullOrWhiteSpace(row.LogoUrl)
            ? null
            : AppendCacheBuster(publicUrls.ToPublicUrl(row.LogoUrl), row.UpdatedAt),
        row.UpdatedAt);

    private static string? AppendCacheBuster(string? url, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        var sep = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{sep}v={updatedAt.Ticks}";
    }
}
