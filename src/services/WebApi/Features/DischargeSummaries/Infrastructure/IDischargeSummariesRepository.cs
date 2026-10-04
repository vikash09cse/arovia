namespace WebApi.Features.DischargeSummaries.Infrastructure;

public class DischargeSummaryGetRow
{
    public Guid AdmissionId { get; set; }
    public string AdmissionCode { get; set; } = "";
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public string Ward { get; set; } = "";
    public string? Bed { get; set; }
    public string RoomClass { get; set; } = "";
    public byte AdmissionStatus { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = "";
    public string PatientFirstName { get; set; } = "";
    public string PatientLastName { get; set; } = "";
    public int? PatientAge { get; set; }
    public byte PatientGender { get; set; }
    public byte[]? PatientPhoneCipher { get; set; }
    public byte[]? PatientAddressCipher { get; set; }
    public Guid AttendingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = "";
    public string DoctorLastName { get; set; } = "";
    public string? DoctorDesignation { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string HospitalName { get; set; } = "";
    public string? HospitalAddress { get; set; }
    public string? HospitalPhone { get; set; }
    public string? HospitalLogoUrl { get; set; }
    public string? HospitalWebsite { get; set; }
    public Guid? DischargeSummaryId { get; set; }
    public DateTime? DateOfSurgery { get; set; }
    public DateTime? DateOfDischarge { get; set; }
    public string? FinalDiagnosis { get; set; }
    public string? DiagnosisKey { get; set; }
    public int? FormSchemaVersion { get; set; }
    public string? FormJson { get; set; }
    public DateTime? SummaryCreatedAt { get; set; }
    public DateTime? SummaryUpdatedAt { get; set; }
}

public class DiagnosisMasterRow
{
    public Guid DiagnosisMasterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PackJson { get; set; }
    public int SortOrder { get; set; }
}

public interface IDischargeSummariesRepository
{
    Task<DischargeSummaryGetRow?> GetAsync(Guid tenantId, Guid admissionId, CancellationToken ct);
    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid admissionId,
        DateOnly? dateOfSurgery,
        DateOnly? dateOfDischarge,
        string? finalDiagnosis,
        string? diagnosisKey,
        int formSchemaVersion,
        string formJson,
        Guid actorId,
        CancellationToken ct);
    Task<IReadOnlyList<DiagnosisMasterRow>> GetActiveDiagnosesAsync(Guid tenantId, CancellationToken ct);
}
