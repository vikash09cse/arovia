using SharedKernel.Enums;

namespace WebApi.Features.Auth.Infrastructure;

public interface IAuthRepository
{
    Task<UserLoginRow?> GetUserForLoginAsync(string email, Guid? tenantId, CancellationToken ct);
    Task<IReadOnlyList<UserLoginRow>> GetTenantUsersForLoginByEmailAsync(string email, CancellationToken ct);
    Task<TenantRow?> GetTenantBySubdomainAsync(string subdomain, CancellationToken ct);
    Task LogLoginAttemptAsync(Guid? tenantId, string userIdentifier, LoginType loginType, bool success, string? failureReason, string? ipAddress, CancellationToken ct);
    Task SaveRefreshTokenAsync(Guid userId, Guid? tenantId, string tokenHash, DateTime expiresAt, CancellationToken ct);
    Task UpdateUserLastLoginAsync(Guid userId, CancellationToken ct);
    Task<MyProfileRow?> GetMyProfileAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<MyProfileRow?> UpdateMyProfileAsync(
        Guid tenantId, Guid userId, string firstName, string lastName, string? designation, CancellationToken ct);
    Task SetMyPasswordAsync(Guid tenantId, Guid userId, string passwordHash, CancellationToken ct);
}
