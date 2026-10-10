namespace WebApi.Features.Doctors;

public record CreateDoctorRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid DepartmentId,
    string? TemporaryPassword,
    string? PhoneNumber,
    string? EmergencyContactNumber);

public record UpdateDoctorRequest(
    string FirstName,
    string LastName,
    Guid DepartmentId,
    string? PhoneNumber,
    string? EmergencyContactNumber);

public record DoctorResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    string? EmergencyContactNumber,
    Guid? DepartmentId,
    string? DepartmentName,
    string Status,
    byte StatusCode,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

public record DoctorListResponse(
    IEnumerable<DoctorResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DoctorLookupItem(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    Guid? DepartmentId,
    string? DepartmentName);
