namespace WebApi.Features.Patients;

public record SavePatientRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    int? Age,
    byte Gender,
    string Phone,
    string Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Email,
    byte? BloodGroup,
    string? ReferredBy);

public record PatientResponse(
    Guid Id,
    string PatientCode,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    int? Age,
    string Gender,
    byte GenderCode,
    string Phone,
    string? Email,
    string Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BloodGroup,
    byte? BloodGroupCode,
    string? ReferredBy,
    string Status,
    byte StatusCode,
    DateTime CreatedAt);

public record PatientListItem(
    Guid Id,
    string PatientCode,
    string FirstName,
    string LastName,
    string Phone,
    string Gender,
    byte GenderCode,
    string Status,
    byte StatusCode,
    DateTime CreatedAt);

public record PatientListResponse(
    IEnumerable<PatientListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DuplicatePhoneResponse(
    Guid PatientId,
    string PatientCode,
    string FirstName,
    string LastName);
