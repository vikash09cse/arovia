using System.Text.Json;

namespace WebApi.Features.DischargeSummaries;

public record SaveDischargeSummaryRequest(
    DateOnly? DateOfSurgery,
    DateOnly? DateOfDischarge,
    string? FinalDiagnosis,
    string? DiagnosisKey,
    int FormSchemaVersion,
    JsonElement Form);

public record DischargeSummaryAdmissionHeader(
    Guid AdmissionId,
    string AdmissionCode,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    string Ward,
    string? Bed,
    string RoomClass,
    byte StatusCode,
    string Status,
    Guid PatientId,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    string PatientFullName,
    int? PatientAge,
    byte PatientGender,
    string PatientGenderLabel,
    string? PatientPhone,
    string? PatientAddress,
    Guid AttendingDoctorId,
    string DoctorName,
    string? DoctorDesignation,
    Guid? DepartmentId,
    string? DepartmentName,
    string HospitalName,
    string? HospitalAddress,
    string? HospitalPhone,
    string? HospitalLogoUrl,
    string? HospitalWebsite);

public record DischargeSummaryPdfResponse(
    Guid AdmissionId,
    string AdmissionCode,
    byte[] Bytes);

public record DischargeSummaryResponse(
    DischargeSummaryAdmissionHeader Admission,
    Guid? DischargeSummaryId,
    bool Exists,
    DateOnly? DateOfSurgery,
    DateOnly? DateOfDischarge,
    string? FinalDiagnosis,
    string? DiagnosisKey,
    int FormSchemaVersion,
    JsonElement Form);

public record DischargeSummaryPrintResponse(
    Guid AdmissionId,
    string AdmissionCode,
    string Html);
