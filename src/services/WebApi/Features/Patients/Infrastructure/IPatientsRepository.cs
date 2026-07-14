namespace WebApi.Features.Patients.Infrastructure;

public class PatientRow
{
    public Guid PatientId { get; set; }
    public Guid TenantId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age { get; set; }
    public byte Gender { get; set; }
    public byte? BloodGroup { get; set; }
    public string? ReferredBy { get; set; }
    public byte PatientStatus { get; set; }
    public byte[] PhoneCipher { get; set; } = [];
    public byte[]? EmailCipher { get; set; }
    public byte[] AddressCipher { get; set; } = [];
    public byte[]? EmergencyNameCipher { get; set; }
    public byte[]? EmergencyPhoneCipher { get; set; }
    public Guid RegisteredBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class DuplicatePhoneRow
{
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public record EncryptedPatientPayload(
    byte[] PhoneCipher,
    byte[]? EmailCipher,
    byte[] AddressCipher,
    byte[]? EmergencyNameCipher,
    byte[]? EmergencyPhoneCipher,
    byte[] PhoneBlindIndex,
    byte[]? EmailBlindIndex);

public interface IPatientsRepository
{
    Task<(IEnumerable<PatientRow> Items, int Total)> GetPatientsAsync(
        Guid tenantId, int page, int pageSize, string? patientCode, byte[]? phoneBlindIndex, byte? status, byte? gender,
        DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct);

    Task<PatientRow?> GetByIdAsync(Guid tenantId, Guid patientId, CancellationToken ct);

    Task<DuplicatePhoneRow?> PhoneExistsAsync(
        Guid tenantId, byte[] phoneBlindIndex, Guid? excludePatientId, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? patientId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        int? age,
        byte gender,
        byte? bloodGroup,
        string? referredBy,
        EncryptedPatientPayload encrypted,
        Guid actorId,
        CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid patientId, byte status, Guid updatedBy, CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid patientId, Guid updatedBy, CancellationToken ct);
}
