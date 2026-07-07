using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.Doctors.Infrastructure;

namespace WebApi.Features.Doctors;

public class DoctorsService(
    IDoctorsRepository repository,
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

    public async Task<Result<DoctorListResponse>> GetDoctorsAsync(
        int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DoctorListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetListAsync(
            tenantId, page, pageSize,
            string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            status, ct);

        return Result<DoctorListResponse>.Ok(new DoctorListResponse(
            items.Select(Map), total, page, pageSize));
    }

    public async Task<Result<IEnumerable<DoctorLookupItem>>> GetActiveDoctorsAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<DoctorLookupItem>>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var rows = await repository.GetActiveAsync(tenantId, ct);
        var mapped = rows.Select(d => new DoctorLookupItem(
            d.UserId, d.FirstName, d.LastName, $"{d.FirstName} {d.LastName}".Trim()));
        return Result<IEnumerable<DoctorLookupItem>>.Ok(mapped);
    }

    public async Task<Result<DoctorResponse>> GetByIdAsync(Guid doctorId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DoctorResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetByIdAsync(tenantId, doctorId, ct);
        if (row == null)
            return Result<DoctorResponse>.Fail(ErrorCode.NotFound, "Doctor not found.");

        return Result<DoctorResponse>.Ok(Map(row));
    }

    public async Task<Result<DoctorResponse>> CreateAsync(CreateDoctorRequest request, CancellationToken ct)
    {
        var validation = ValidateNameEmail(request.FirstName, request.LastName, request.Email);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<DoctorResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var email = request.Email.Trim();

        if (await repository.EmailExistsAsync(tenantId, email, null, ct))
            return Result<DoctorResponse>.Fail(ErrorCode.AlreadyExists, "Email is already in use.");

        var password = request.TemporaryPassword ?? PasswordHelper.GenerateTemporaryPassword();
        var id = await repository.CreateAsync(
            tenantId, email, request.FirstName.Trim(), request.LastName.Trim(),
            PasswordHelper.Hash(password), GetUserId(), ct);

        var created = await repository.GetByIdAsync(tenantId, id, ct);
        return Result<DoctorResponse>.Ok(Map(created!), "Doctor created successfully.");
    }

    public async Task<Result<DoctorResponse>> UpdateAsync(Guid doctorId, UpdateDoctorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Result<DoctorResponse>.Fail(ErrorCode.Validation, "First name and last name are required.");

        var tenantError = RequireTenantContext<DoctorResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, doctorId, ct);
        if (existing == null)
            return Result<DoctorResponse>.Fail(ErrorCode.NotFound, "Doctor not found.");

        await repository.UpdateAsync(tenantId, doctorId, request.FirstName.Trim(), request.LastName.Trim(), GetUserId(), ct);
        var updated = await repository.GetByIdAsync(tenantId, doctorId, ct);
        return Result<DoctorResponse>.Ok(Map(updated!), "Doctor updated successfully.");
    }

    public async Task<Result<bool>> SetStatusAsync(Guid doctorId, UserStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, doctorId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Doctor not found.");

        await repository.SetStatusAsync(tenantId, doctorId, (byte)status, GetUserId(), ct);
        return Result<bool>.Ok(true, "Doctor status updated.");
    }

    private static Result<DoctorResponse>? ValidateNameEmail(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Result<DoctorResponse>.Fail(ErrorCode.Validation, "First name and last name are required.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<DoctorResponse>.Fail(ErrorCode.Validation, "A valid email is required.");

        return null;
    }

    private static DoctorResponse Map(DoctorRow row) => new(
        row.UserId,
        row.Email,
        row.FirstName,
        row.LastName,
        $"{row.FirstName} {row.LastName}".Trim(),
        row.Status == (byte)UserStatus.Active ? "Active" : "Inactive",
        row.Status,
        row.LastLoginAt,
        row.CreatedAt);
}
