using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.VisitAddons.Infrastructure;

namespace WebApi.Features.VisitAddons;

public class VisitAddonsService(
    IVisitAddonsRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;

    public async Task<Result<VisitAddonListResponse>> GetListAsync(
        int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<VisitAddonListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await repository.GetListAsync(
            GetTenantId(), page, pageSize,
            string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            status, ct);

        return Result<VisitAddonListResponse>.Ok(new VisitAddonListResponse(
            items.Select(Map), total, page, pageSize));
    }

    public async Task<Result<IEnumerable<VisitAddonLookupItem>>> GetActiveAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<VisitAddonLookupItem>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetActiveAsync(GetTenantId(), ct);
        var mapped = rows.Select(r => new VisitAddonLookupItem(
            r.VisitAddonId, r.Name, r.Code, r.DefaultAmount));
        return Result<IEnumerable<VisitAddonLookupItem>>.Ok(mapped);
    }

    public async Task<Result<VisitAddonResponse>> GetByIdAsync(Guid visitAddonId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<VisitAddonResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetByIdAsync(GetTenantId(), visitAddonId, ct);
        if (row == null)
            return Result<VisitAddonResponse>.Fail(ErrorCode.NotFound, "Visit addon not found.");

        return Result<VisitAddonResponse>.Ok(Map(row));
    }

    public async Task<Result<VisitAddonResponse>> CreateAsync(CreateVisitAddonRequest request, CancellationToken ct)
    {
        var validation = ValidateRequest(request.Name, request.Code, request.DefaultAmount);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<VisitAddonResponse>();
        if (tenantError != null) return tenantError;

        var id = await repository.SaveAsync(
            GetTenantId(), null, request.Name.Trim(),
            TrimOrNull(request.Code), request.DefaultAmount,
            GetUserId(), ct);

        var created = await repository.GetByIdAsync(GetTenantId(), id, ct);
        return Result<VisitAddonResponse>.Ok(Map(created!), "Visit addon created successfully.");
    }

    public async Task<Result<VisitAddonResponse>> UpdateAsync(
        Guid visitAddonId, UpdateVisitAddonRequest request, CancellationToken ct)
    {
        var validation = ValidateRequest(request.Name, request.Code, request.DefaultAmount);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<VisitAddonResponse>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), visitAddonId, ct);
        if (existing == null)
            return Result<VisitAddonResponse>.Fail(ErrorCode.NotFound, "Visit addon not found.");

        await repository.SaveAsync(
            GetTenantId(), visitAddonId, request.Name.Trim(),
            TrimOrNull(request.Code), request.DefaultAmount,
            GetUserId(), ct);

        var updated = await repository.GetByIdAsync(GetTenantId(), visitAddonId, ct);
        return Result<VisitAddonResponse>.Ok(Map(updated!), "Visit addon updated successfully.");
    }

    public async Task<Result<bool>> SetStatusAsync(Guid visitAddonId, VisitAddonStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), visitAddonId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Visit addon not found.");

        await repository.SetStatusAsync(GetTenantId(), visitAddonId, (byte)status, GetUserId(), ct);
        return Result<bool>.Ok(true, "Visit addon status updated.");
    }

    private static Result<VisitAddonResponse>? ValidateRequest(string name, string? code, decimal defaultAmount)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<VisitAddonResponse>.Fail(ErrorCode.Validation, "Addon name is required.");

        if (name.Trim().Length > 200)
            return Result<VisitAddonResponse>.Fail(ErrorCode.Validation, "Addon name cannot exceed 200 characters.");

        if (code?.Length > 50)
            return Result<VisitAddonResponse>.Fail(ErrorCode.Validation, "Addon code cannot exceed 50 characters.");

        if (defaultAmount < 0 || defaultAmount > 999999.99m)
            return Result<VisitAddonResponse>.Fail(ErrorCode.Validation, "Invalid addon amount.");

        return null;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VisitAddonResponse Map(VisitAddonRow row) => new(
        row.VisitAddonId,
        row.Name,
        row.Code,
        row.DefaultAmount,
        row.AddonStatus == (byte)VisitAddonStatus.Active ? "Active" : "Inactive",
        row.AddonStatus,
        row.CreatedAt,
        row.UpdatedAt);
}
