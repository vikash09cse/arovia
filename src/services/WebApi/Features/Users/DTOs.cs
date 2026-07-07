namespace WebApi.Features.Users;

public record CreateTenantUserRequest(
    string Email,
    string FirstName,
    string LastName,
    byte Role,
    string? TemporaryPassword);

public record UpdateTenantUserRequest(
    string FirstName,
    string LastName,
    byte Role);

public record TenantUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    byte RoleCode,
    string Status,
    byte StatusCode,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record TenantUserListResponse(
    IEnumerable<TenantUserResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
