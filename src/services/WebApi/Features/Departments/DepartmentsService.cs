using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Departments.Infrastructure;

namespace WebApi.Features.Departments;

public class DepartmentsService(
    IDepartmentsRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<Result<DepartmentListResponse>> GetListAsync(
        int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DepartmentListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await repository.GetListAsync(
            GetTenantId(), page, pageSize,
            string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            status, ct);

        return Result<DepartmentListResponse>.Ok(
            new DepartmentListResponse(items.Select(Map), total, page, pageSize));
    }

    public async Task<Result<IEnumerable<DepartmentLookupItem>>> GetActiveAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<DepartmentLookupItem>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetActiveAsync(GetTenantId(), ct);
        return Result<IEnumerable<DepartmentLookupItem>>.Ok(
            rows.Select(r => new DepartmentLookupItem(r.DepartmentId, r.Name)));
    }

    public async Task<Result<DepartmentResponse>> GetByIdAsync(Guid departmentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DepartmentResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetByIdAsync(GetTenantId(), departmentId, ct);
        if (row == null)
            return Result<DepartmentResponse>.Fail(ErrorCode.NotFound, "Department not found.");

        return Result<DepartmentResponse>.Ok(Map(row));
    }

    public async Task<Result<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct)
    {
        var validation = ValidateName(request.Name);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<DepartmentResponse>();
        if (tenantError != null) return tenantError;

        var id = await repository.SaveAsync(GetTenantId(), null, request.Name.Trim(), GetUserId(), ct);
        var created = await repository.GetByIdAsync(GetTenantId(), id, ct);
        return Result<DepartmentResponse>.Ok(Map(created!), "Department created successfully.");
    }

    public async Task<Result<DepartmentResponse>> UpdateAsync(
        Guid departmentId, UpdateDepartmentRequest request, CancellationToken ct)
    {
        var validation = ValidateName(request.Name);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<DepartmentResponse>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), departmentId, ct);
        if (existing == null)
            return Result<DepartmentResponse>.Fail(ErrorCode.NotFound, "Department not found.");

        await repository.SaveAsync(GetTenantId(), departmentId, request.Name.Trim(), GetUserId(), ct);
        var updated = await repository.GetByIdAsync(GetTenantId(), departmentId, ct);
        return Result<DepartmentResponse>.Ok(Map(updated!), "Department updated successfully.");
    }

    public async Task<Result<bool>> SetStatusAsync(Guid departmentId, DepartmentStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), departmentId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Department not found.");

        await repository.SetStatusAsync(GetTenantId(), departmentId, (byte)status, GetUserId(), ct);
        return Result<bool>.Ok(true, "Department status updated.");
    }

    private static Result<DepartmentResponse>? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<DepartmentResponse>.Fail(ErrorCode.Validation, "Department name is required.");
        if (name.Trim().Length > 150)
            return Result<DepartmentResponse>.Fail(ErrorCode.Validation, "Department name cannot exceed 150 characters.");
        return null;
    }

    private static DepartmentResponse Map(DepartmentRow row) => new(
        row.DepartmentId,
        row.Name,
        row.DepartmentStatus == (byte)DepartmentStatus.Active ? "Active" : "Inactive",
        row.DepartmentStatus,
        row.CreatedAt,
        row.UpdatedAt);

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;
}
