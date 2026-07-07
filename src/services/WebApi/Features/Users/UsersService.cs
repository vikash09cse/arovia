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
        var id = await repository.CreateAsync(
            tenantId, request.Email.Trim(), request.FirstName, request.LastName,
            request.Role, PasswordHelper.Hash(password), GetUserId(), ct);

        var created = await repository.GetByIdAsync(tenantId, id, ct);
        return Result<TenantUserResponse>.Ok(Map(created!), "User created successfully.");
    }

    public async Task<Result<TenantUserResponse>> UpdateUserAsync(Guid userId, UpdateTenantUserRequest request, CancellationToken ct)
    {
        if (request.Role is not ((byte)UserType.Staff) and not ((byte)UserType.Doctor))
            return Result<TenantUserResponse>.Fail(ErrorCode.Validation, "Only Staff and Doctor roles are allowed.");

        var tenantError = RequireTenantContext<TenantUserResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, userId, ct);
        if (existing == null)
            return Result<TenantUserResponse>.Fail(ErrorCode.NotFound, "User not found.");

        await repository.UpdateAsync(tenantId, userId, request.FirstName, request.LastName, request.Role, GetUserId(), ct);
        var updated = await repository.GetByIdAsync(tenantId, userId, ct);
        return Result<TenantUserResponse>.Ok(Map(updated!), "User updated successfully.");
    }

    public async Task<Result<bool>> SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, userId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "User not found.");

        await repository.SetStatusAsync(tenantId, userId, status, GetUserId(), ct);
        return Result<bool>.Ok(true, "User status updated.");
    }

    private static TenantUserResponse Map(TenantUserRow row) => new(
        row.UserId, row.Email, row.FirstName, row.LastName,
        RoleNames.FromUserType((UserType)row.Role), row.Role,
        row.Status == (byte)UserStatus.Active ? "Active" : "Inactive", row.Status,
        row.LastLoginAt, row.CreatedAt);
}
