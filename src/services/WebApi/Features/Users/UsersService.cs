using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.Users.Infrastructure;

namespace WebApi.Features.Users;

public class UsersService(
    IUsersRepository repository,
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

    public async Task<Result<TenantUserListResponse>> GetUsersAsync(int page, int pageSize, string? filter, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<TenantUserListResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetUsersAsync(tenantId, page, pageSize, filter, ct);
        var mapped = items.Select(Map);
        return Result<TenantUserListResponse>.Ok(new TenantUserListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<TenantUserResponse>> CreateUserAsync(CreateTenantUserRequest request, CancellationToken ct)
    {
        if (request.Role is not ((byte)UserType.Staff) and not ((byte)UserType.Doctor))
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "Only Staff and Doctor roles can be created.");

        var tenantError = RequireTenantContext<TenantUserResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        if (await repository.EmailExistsAsync(tenantId, request.Email.Trim(), null, ct))
            return Result<TenantUserResponse>.Fail(ErrorCode.AlreadyExists, "Email is already in use.");

        var password = request.TemporaryPassword ?? PasswordHelper.GenerateTemporaryPassword();
        var designation = string.IsNullOrWhiteSpace(request.Designation) ? null : request.Designation.Trim();
        var id = await repository.CreateAsync(
            tenantId, request.Email.Trim(), request.FirstName, request.LastName, designation,
            request.Role, PasswordHelper.Hash(password), GetUserId(), ct);

        var created = await repository.GetByIdAsync(tenantId, id, ct);
        return Result<TenantUserResponse>.Ok(Map(created!), "User created successfully.");
    }

    public async Task<Result<TenantUserResponse>> UpdateUserAsync(Guid userId, UpdateTenantUserRequest request, CancellationToken ct)
    {
        if (request.Role is not ((byte)UserType.Staff) and not ((byte)UserType.Doctor))
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "Only Staff and Doctor roles are allowed.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Trim().Length < 2)
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "First name must be at least 2 characters.");

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Trim().Length < 2)
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "Last name must be at least 2 characters.");

        var tenantError = RequireTenantContext<TenantUserResponse>();
        if (tenantError != null) return tenantError;

        if (userId == GetUserId())
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "You cannot edit your own account here.");

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, userId, ct);
        if (existing == null)
            return Result<TenantUserResponse>.Fail(ErrorCode.NotFound, "User not found.");

        if (existing.Role is ((byte)UserType.TenantSuperAdmin) or ((byte)UserType.PlatformAdmin))
            return Result<TenantUserResponse>.Fail(ErrorCode.Forbidden, "Admin accounts cannot be edited here.");

        var designation = string.IsNullOrWhiteSpace(request.Designation) ? null : request.Designation.Trim();
        await repository.UpdateAsync(
            tenantId, userId, request.FirstName.Trim(), request.LastName.Trim(), designation, request.Role, GetUserId(), ct);
        var updated = await repository.GetByIdAsync(tenantId, userId, ct);
        return Result<TenantUserResponse>.Ok(Map(updated!), "User updated successfully.");
    }

    public async Task<Result<bool>> SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        if (userId == GetUserId())
            return Result<bool>.Fail(ErrorCode.Validation, "You cannot change your own status here.");

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, userId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "User not found.");

        if (existing.Role is ((byte)UserType.TenantSuperAdmin) or ((byte)UserType.PlatformAdmin))
            return Result<bool>.Fail(ErrorCode.Forbidden, "Admin accounts cannot be modified here.");

        await repository.SetStatusAsync(tenantId, userId, status, GetUserId(), ct);
        return Result<bool>.Ok(true, "User status updated.");
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        if (userId == GetUserId())
            return Result<bool>.Fail(ErrorCode.Validation, "You cannot delete your own account.");

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, userId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "User not found.");

        if (existing.Role is not ((byte)UserType.Staff) and not ((byte)UserType.Doctor))
            return Result<bool>.Fail(ErrorCode.Forbidden, "Only Staff and Doctor accounts can be deleted.");

        await repository.DeleteAsync(tenantId, userId, GetUserId(), ct);
        return Result<bool>.Ok(true, "User deleted successfully.");
    }

    private static TenantUserResponse Map(TenantUserRow row) => new(
        row.UserId, row.Email, row.FirstName, row.LastName, row.Designation,
        RoleNames.FromUserType((UserType)row.Role), row.Role,
        row.Status == (byte)UserStatus.Active ? "Active" : "Inactive", row.Status,
        row.LastLoginAt, row.CreatedAt);
}
