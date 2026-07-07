using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.LabTests.Infrastructure;

namespace WebApi.Features.LabTests;

public class LabTestsService(
    ILabTestsRepository repository,
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

    public async Task<Result<LabAgencyListResponse>> GetListAsync(
        int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<LabAgencyListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await repository.GetListAsync(
            GetTenantId(), page, pageSize,
            string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            status, ct);

        return Result<LabAgencyListResponse>.Ok(new LabAgencyListResponse(
            items.Select(Map), total, page, pageSize));
    }

    public async Task<Result<IEnumerable<LabAgencyLookupItem>>> GetActiveAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<LabAgencyLookupItem>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetActiveAsync(GetTenantId(), ct);
        var mapped = rows.Select(r => new LabAgencyLookupItem(
            r.LabAgencyId, r.Name, r.ContactPerson, r.Phone));
        return Result<IEnumerable<LabAgencyLookupItem>>.Ok(mapped);
    }

    public async Task<Result<LabAgencyResponse>> GetByIdAsync(Guid labAgencyId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<LabAgencyResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetByIdAsync(GetTenantId(), labAgencyId, ct);
        if (row == null)
            return Result<LabAgencyResponse>.Fail(ErrorCode.NotFound, "Lab agency not found.");

        return Result<LabAgencyResponse>.Ok(Map(row));
    }

    public async Task<Result<LabAgencyResponse>> CreateAsync(CreateLabAgencyRequest request, CancellationToken ct)
    {
        var validation = ValidateAgencyRequest(request.Name, request.Email, request.Phone, request.Notes);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<LabAgencyResponse>();
        if (tenantError != null) return tenantError;

        var id = await repository.SaveAsync(
            GetTenantId(), null, request.Name.Trim(),
            TrimOrNull(request.ContactPerson),
            TrimOrNull(request.Phone),
            TrimOrNull(request.Email),
            TrimOrNull(request.Address),
            TrimOrNull(request.Notes),
            GetUserId(), ct);

        var created = await repository.GetByIdAsync(GetTenantId(), id, ct);
        return Result<LabAgencyResponse>.Ok(Map(created!), "Lab agency created successfully.");
    }

    public async Task<Result<LabAgencyResponse>> UpdateAsync(
        Guid labAgencyId, UpdateLabAgencyRequest request, CancellationToken ct)
    {
        var validation = ValidateAgencyRequest(request.Name, request.Email, request.Phone, request.Notes);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<LabAgencyResponse>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), labAgencyId, ct);
        if (existing == null)
            return Result<LabAgencyResponse>.Fail(ErrorCode.NotFound, "Lab agency not found.");

        await repository.SaveAsync(
            GetTenantId(), labAgencyId, request.Name.Trim(),
            TrimOrNull(request.ContactPerson),
            TrimOrNull(request.Phone),
            TrimOrNull(request.Email),
            TrimOrNull(request.Address),
            TrimOrNull(request.Notes),
            GetUserId(), ct);

        var updated = await repository.GetByIdAsync(GetTenantId(), labAgencyId, ct);
        return Result<LabAgencyResponse>.Ok(Map(updated!), "Lab agency updated successfully.");
    }

    public async Task<Result<bool>> SetStatusAsync(Guid labAgencyId, LabAgencyStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), labAgencyId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Lab agency not found.");

        await repository.SetStatusAsync(GetTenantId(), labAgencyId, (byte)status, GetUserId(), ct);
        return Result<bool>.Ok(true, "Lab agency status updated.");
    }

    public async Task<Result<VisitLabAgencyResponse>> AssignToVisitAsync(
        Guid visitId, AssignVisitLabAgencyRequest request, CancellationToken ct)
    {
        if (request.LabAgencyId == Guid.Empty)
            return Result<VisitLabAgencyResponse>.Fail(ErrorCode.Validation, "Lab agency is required.");

        if (request.Notes?.Length > 500)
            return Result<VisitLabAgencyResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<VisitLabAgencyResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.AssignToVisitAsync(
            GetTenantId(), visitId, request.LabAgencyId,
            TrimOrNull(request.Notes), GetUserId(), ct);

        if (row == null)
            return Result<VisitLabAgencyResponse>.Fail(ErrorCode.Validation, "Unable to assign lab agency.");

        return Result<VisitLabAgencyResponse>.Ok(MapVisitAssignment(row), "Lab agency assigned.");
    }

    public async Task<Result<bool>> RemoveFromVisitAsync(
        Guid visitId, Guid assignmentId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        await repository.RemoveFromVisitAsync(GetTenantId(), visitId, assignmentId, GetUserId(), ct);
        return Result<bool>.Ok(true, "Lab agency assignment removed.");
    }

    public static VisitLabAgencyResponse MapVisitAssignment(VisitLabAgencyRow row)
    {
        var assignerName = $"{row.AssignerFirstName} {row.AssignerLastName}".Trim();
        if (string.IsNullOrWhiteSpace(assignerName))
            assignerName = null;

        return new VisitLabAgencyResponse(
            row.VisitLabAgencyId,
            row.LabAgencyId,
            row.AgencyName,
            row.AssignedAt,
            row.AssignedByUserId,
            assignerName,
            row.Notes);
    }

    private static Result<LabAgencyResponse>? ValidateAgencyRequest(
        string name, string? email, string? phone, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<LabAgencyResponse>.Fail(ErrorCode.Validation, "Agency name is required.");

        if (name.Trim().Length > 200)
            return Result<LabAgencyResponse>.Fail(ErrorCode.Validation, "Agency name cannot exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(email) && (!email.Contains('@') || email.Length > 256))
            return Result<LabAgencyResponse>.Fail(ErrorCode.Validation, "A valid email is required.");

        if (phone?.Length > 20)
            return Result<LabAgencyResponse>.Fail(ErrorCode.Validation, "Phone cannot exceed 20 characters.");

        if (notes?.Length > 500)
            return Result<LabAgencyResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        return null;
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LabAgencyResponse Map(LabAgencyRow row) => new(
        row.LabAgencyId,
        row.Name,
        row.ContactPerson,
        row.Phone,
        row.Email,
        row.Address,
        row.Notes,
        row.AgencyStatus == (byte)LabAgencyStatus.Active ? "Active" : "Inactive",
        row.AgencyStatus,
        row.CreatedAt,
        row.UpdatedAt);
}
