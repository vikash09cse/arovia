namespace WebApi.Features.Users;

public record CreateTenantUserRequest(
    string Email,
    string FirstName,
    string LastName,
    byte Role,
    string? TemporaryPassword,
    string? Designation,
    string? PhoneNumber,
    string? EmergencyContactNumber);

public record UpdateTenantUserRequest(
    string FirstName,
    string LastName,
    byte Role,
    string? Designation,
    string? PhoneNumber,
    string? EmergencyContactNumber);

public record SetUserPasswordRequest(string NewPassword);

public record TenantUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Designation,
    string? PhoneNumber,
    string? EmergencyContactNumber,
    string Role,
    byte RoleCode,
    string Status,
    byte StatusCode,
    decimal? MonthlySalary,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record TenantUserListResponse(
    IEnumerable<TenantUserResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
