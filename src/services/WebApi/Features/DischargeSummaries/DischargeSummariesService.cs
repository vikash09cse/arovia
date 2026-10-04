using System.Text.Json;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.DischargeSummaries.Infrastructure;

namespace WebApi.Features.DischargeSummaries;

public class DischargeSummariesService(
    IDischargeSummariesRepository repository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment,
    PhiEncryptionHelper encryption,
    PublicUrlHelper publicUrls)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<Result<DischargeSummaryResponse>> GetAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DischargeSummaryResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetAsync(tenantId, admissionId, ct);
        if (row == null)
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.NotFound, "Admission not found.");

        return Result<DischargeSummaryResponse>.Ok(MapResponse(row));
    }

    public async Task<Result<DischargeSummaryResponse>> SaveAsync(
        Guid admissionId, SaveDischargeSummaryRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DischargeSummaryResponse>();
        if (tenantError != null) return tenantError;

        if (request.Form.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, "Form payload is required.");

        if (request.FormSchemaVersion < 1)
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, "Form schema version must be >= 1.");

        var formJson = JsonSerializer.Serialize(request.Form, JsonOptions);
        if (formJson.Length > 2_000_000)
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, "Form payload is too large.");

        var finalDiagnosis = string.IsNullOrWhiteSpace(request.FinalDiagnosis)
            ? null
            : request.FinalDiagnosis.Trim();
        if (finalDiagnosis is { Length: > 500 })
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, "Final diagnosis is too long.");

        var diagnosisKey = string.IsNullOrWhiteSpace(request.DiagnosisKey)
            ? null
            : request.DiagnosisKey.Trim();
        if (diagnosisKey is { Length: > 100 })
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, "Diagnosis key is too long.");

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        try
        {
            await repository.SaveAsync(
                tenantId,
                admissionId,
                request.DateOfSurgery,
                request.DateOfDischarge,
                finalDiagnosis,
                diagnosisKey,
                request.FormSchemaVersion,
                formJson,
                GetUserId(),
                ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50401)
        {
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.NotFound, "Admission not found.");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 50402 or 50403)
        {
            return Result<DischargeSummaryResponse>.Fail(ErrorCode.Validation, ex.Message);
        }

        return await GetAsync(admissionId, ct);
    }

    public async Task<Result<DischargeSummaryPrintResponse>> GetPrintHtmlAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DischargeSummaryPrintResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetAsync(tenantId, admissionId, ct);
        if (row == null)
            return Result<DischargeSummaryPrintResponse>.Fail(ErrorCode.NotFound, "Admission not found.");

        var response = MapResponse(row);
        var logoBytes = TryReadLogoBytes(row.HospitalLogoUrl);
        var html = DischargeSummaryPrintBuilder.Build(response, logoBytes);
        return Result<DischargeSummaryPrintResponse>.Ok(new DischargeSummaryPrintResponse(
            response.Admission.AdmissionId,
            response.Admission.AdmissionCode,
            html));
    }

    public async Task<Result<DischargeSummaryPdfResponse>> GetPrintPdfAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DischargeSummaryPdfResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetAsync(tenantId, admissionId, ct);
        if (row == null)
            return Result<DischargeSummaryPdfResponse>.Fail(ErrorCode.NotFound, "Admission not found.");

        var response = MapResponse(row);
        var logoBytes = TryReadLogoBytes(row.HospitalLogoUrl);
        var bytes = DischargeSummaryPdfBuilder.Build(response, logoBytes);
        return Result<DischargeSummaryPdfResponse>.Ok(new DischargeSummaryPdfResponse(
            response.Admission.AdmissionId,
            response.Admission.AdmissionCode,
            bytes));
    }

    private DischargeSummaryResponse MapResponse(DischargeSummaryGetRow row)
    {
        var exists = row.DischargeSummaryId.HasValue;
        JsonElement form;
        if (exists && !string.IsNullOrWhiteSpace(row.FormJson))
        {
            form = JsonDocument.Parse(row.FormJson).RootElement.Clone();
        }
        else
        {
            form = JsonDocument.Parse(BuildEmptyFormJson(row)).RootElement.Clone();
        }

        DateOnly? surgery = row.DateOfSurgery.HasValue
            ? DateOnly.FromDateTime(row.DateOfSurgery.Value)
            : null;
        DateOnly? discharge = row.DateOfDischarge.HasValue
            ? DateOnly.FromDateTime(row.DateOfDischarge.Value)
            : null;

        if (!exists)
        {
            surgery = null;
            discharge = row.DischargedAt.HasValue
                ? DateOnly.FromDateTime(row.DischargedAt.Value)
                : DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var header = new DischargeSummaryAdmissionHeader(
            row.AdmissionId,
            row.AdmissionCode,
            row.AdmittedAt,
            row.DischargedAt,
            row.Ward,
            row.Bed,
            row.RoomClass,
            row.AdmissionStatus,
            FormatStatus(row.AdmissionStatus),
            row.PatientId,
            row.PatientCode,
            row.PatientFirstName,
            row.PatientLastName,
            $"{row.PatientFirstName} {row.PatientLastName}".Trim(),
            row.PatientAge,
            row.PatientGender,
            FormatGender(row.PatientGender),
            TryDecrypt(row.PatientPhoneCipher),
            TryDecrypt(row.PatientAddressCipher),
            row.AttendingDoctorId,
            $"{row.DoctorFirstName} {row.DoctorLastName}".Trim(),
            row.DoctorDesignation,
            row.DepartmentId,
            row.DepartmentName,
            row.HospitalName,
            row.HospitalAddress,
            row.HospitalPhone,
            publicUrls.ToPublicUrl(row.HospitalLogoUrl),
            row.HospitalWebsite);

        return new DischargeSummaryResponse(
            header,
            row.DischargeSummaryId,
            exists,
            surgery,
            discharge,
            exists ? row.FinalDiagnosis : null,
            exists ? row.DiagnosisKey : null,
            exists ? (row.FormSchemaVersion ?? 1) : 1,
            form);
    }

    private static string BuildEmptyFormJson(DischargeSummaryGetRow row)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var dischargeDate = row.DischargedAt.HasValue
            ? DateOnly.FromDateTime(row.DischargedAt.Value).ToString("yyyy-MM-dd")
            : today;

        var empty = new
        {
            admissionId = row.AdmissionId,
            dateOfSurgery = "",
            dateOfDischarge = dischargeDate,
            finalDiagnosis = "",
            chiefComplaints = "",
            briefHistory = "",
            investigations = new
            {
                cbc = "",
                kft = "",
                urine = "",
                usgCt = "",
                lft = "",
                vm = "",
                rbs = "",
                bg = "",
                cxrEcg = ""
            },
            investigationDates = new
            {
                cbc = "",
                kft = "",
                urine = "",
                usgCt = "",
                lft = "",
                vm = "",
                rbs = "",
                bg = "",
                cxrEcg = ""
            },
            procedureDetails = "",
            findings = "",
            conditionAtDischarge = "",
            medications = "",
            advice = "Take plenty of oral fluids.\nAvoid heavy work / straining.\nTake medicines on time.\nCome to OPD if fever / vomiting / hematuria.",
            followUp = "",
            fatherName = "",
            pastHistory = new { comorbidities = "", pastSurgery = "", allergy = "", addiction = "" },
            examination = new
            {
                general = "",
                pulse = "",
                bp = "",
                temp = "",
                spo2 = "",
                systemic = "",
                perAbdomen = "",
                genitalia = ""
            },
            treatmentDuringAdmission = "",
            anaesthesia = "",
            assistantSurgeon = "",
            indication = "",
            intraopComplications = "Nil",
            conditionExtras = new
            {
                pulse = "",
                bp = "",
                temp = "",
                spo2 = "",
                urineOutput = "",
                oralIntake = "",
                ambulation = ""
            },
            followUpDate = "",
            followUpDepartment = "Urology OPD",
            followUpInvestigations = "",
            procedureKey = "",
            procedureExtras = new { },
            diagnosisKey = "",
            diagnosisOther = "",
            generalCondition = "Stable",
            generalConditionOther = "",
            postopCourse = "Uneventful",
            postopCourseDetails = "",
            catheterKey = "",
            catheterOther = "",
            specimen = "Not applicable",
            dischargeType = "Routine discharge",
            followUpKey = "Routine OPD follow-up",
            followUpOther = "",
            followUpNotes = "",
            medicines = Array.Empty<object>()
        };

        return JsonSerializer.Serialize(empty, JsonOptions);
    }

    private static string FormatStatus(byte code) => code switch
    {
        (byte)AdmissionStatus.Admitted => "Admitted",
        (byte)AdmissionStatus.Discharged => "Discharged",
        (byte)AdmissionStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    private static string FormatGender(byte code) => code switch
    {
        (byte)Gender.Male => "Male",
        (byte)Gender.Female => "Female",
        (byte)Gender.Other => "Other",
        _ => "—"
    };

    private string? TryDecrypt(byte[]? cipher)
    {
        if (cipher is null || cipher.Length == 0) return null;
        try { return encryption.Decrypt(cipher); }
        catch { return null; }
    }

    private byte[]? TryReadLogoBytes(string? logoUrl)
    {
        var relative = publicUrls.ToWebRootRelativePath(logoUrl);
        if (string.IsNullOrWhiteSpace(relative)) return null;

        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot)) return null;

        var absolutePath = Path.GetFullPath(Path.Combine(webRoot, relative));
        var rootFull = Path.GetFullPath(webRoot);
        if (!absolutePath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) || !File.Exists(absolutePath))
            return null;

        try { return File.ReadAllBytes(absolutePath); }
        catch { return null; }
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
}
