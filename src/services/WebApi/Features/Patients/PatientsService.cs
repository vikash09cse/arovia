using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.Patients.Infrastructure;

namespace WebApi.Features.Patients;

public class PatientsService(
    IPatientsRepository repository,
    PhiEncryptionHelper encryption,
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

    public async Task<Result<PatientListResponse>> GetPatientsAsync(
        int page, int pageSize, string? patientCode, string? phone, byte? status, byte? gender, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PatientListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;

        byte[]? phoneBlindIndex = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = PhiEncryptionHelper.NormalizePhone(phone);
            if (normalized.Length is < 10 or > 15)
                return Result<PatientListResponse>.Fail(ErrorCode.Validation, "Phone search must be 10–15 digits.");
            phoneBlindIndex = encryption.ComputeBlindIndex(tenantId, normalized);
        }

        var (items, total) = await repository.GetPatientsAsync(
            tenantId, page, pageSize,
            string.IsNullOrWhiteSpace(patientCode) ? null : patientCode.Trim(),
            phoneBlindIndex, status, gender, ct);

        var mapped = items.Select(row => MapListItem(row));
        return Result<PatientListResponse>.Ok(new PatientListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(Guid patientId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PatientResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetByIdAsync(tenantId, patientId, ct);
        if (row == null)
            return Result<PatientResponse>.Fail(ErrorCode.NotFound, "Patient not found.");

        return Result<PatientResponse>.Ok(MapDetail(row));
    }

    public async Task<Result<PatientResponse>> CreateAsync(SavePatientRequest request, CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<PatientResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var encrypted = BuildEncryptedPayload(tenantId, request);

        var duplicate = await repository.PhoneExistsAsync(tenantId, encrypted.PhoneBlindIndex, null, ct);
        if (duplicate != null)
            return Result<PatientResponse>.Fail(
                ErrorCode.AlreadyExists,
                $"Phone number already registered to patient {duplicate.PatientCode} ({duplicate.FirstName} {duplicate.LastName}).");

        var id = await repository.SaveAsync(
            tenantId, null,
            request.FirstName.Trim(), request.LastName.Trim(),
            request.DateOfBirth, request.Age, request.Gender,
            request.BloodGroup, NormalizeOptional(request.ReferredBy),
            encrypted, GetUserId(), ct);

        var created = await repository.GetByIdAsync(tenantId, id, ct);
        return Result<PatientResponse>.Ok(MapDetail(created!), "Patient registered successfully.");
    }

    public async Task<Result<PatientResponse>> UpdateAsync(Guid patientId, SavePatientRequest request, CancellationToken ct)
    {
        var validation = ValidateRequest(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<PatientResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, patientId, ct);
        if (existing == null)
            return Result<PatientResponse>.Fail(ErrorCode.NotFound, "Patient not found.");

        var encrypted = BuildEncryptedPayload(tenantId, request);

        var duplicate = await repository.PhoneExistsAsync(tenantId, encrypted.PhoneBlindIndex, patientId, ct);
        if (duplicate != null)
            return Result<PatientResponse>.Fail(
                ErrorCode.AlreadyExists,
                $"Phone number already registered to patient {duplicate.PatientCode} ({duplicate.FirstName} {duplicate.LastName}).");

        await repository.SaveAsync(
            tenantId, patientId,
            request.FirstName.Trim(), request.LastName.Trim(),
            request.DateOfBirth, request.Age, request.Gender,
            request.BloodGroup, NormalizeOptional(request.ReferredBy),
            encrypted, GetUserId(), ct);

        var updated = await repository.GetByIdAsync(tenantId, patientId, ct);
        return Result<PatientResponse>.Ok(MapDetail(updated!), "Patient updated successfully.");
    }

    public async Task<Result<bool>> SetStatusAsync(Guid patientId, PatientStatus status, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, patientId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Patient not found.");

        await repository.SetStatusAsync(tenantId, patientId, (byte)status, GetUserId(), ct);
        return Result<bool>.Ok(true, "Patient status updated.");
    }

    public async Task<Result<bool>> DeleteAsync(Guid patientId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var existing = await repository.GetByIdAsync(tenantId, patientId, ct);
        if (existing == null)
            return Result<bool>.Fail(ErrorCode.NotFound, "Patient not found.");

        await repository.DeleteAsync(tenantId, patientId, GetUserId(), ct);
        return Result<bool>.Ok(true, "Patient deleted successfully.");
    }

    private EncryptedPatientPayload BuildEncryptedPayload(Guid tenantId, SavePatientRequest request)
    {
        var normalizedPhone = PhiEncryptionHelper.NormalizePhone(request.Phone);
        var normalizedEmail = PhiEncryptionHelper.NormalizeEmail(request.Email);
        var emergencyName = string.IsNullOrWhiteSpace(request.EmergencyContactName)
            ? null
            : request.EmergencyContactName.Trim();
        var emergencyPhone = PhiEncryptionHelper.NormalizePhone(request.EmergencyContactPhone ?? string.Empty);
        var hasEmergencyPhone = emergencyPhone.Length > 0;

        return new EncryptedPatientPayload(
            encryption.Encrypt(normalizedPhone),
            normalizedEmail == null ? null : encryption.Encrypt(normalizedEmail),
            encryption.Encrypt(request.Address.Trim()),
            emergencyName == null ? null : encryption.Encrypt(emergencyName),
            hasEmergencyPhone ? encryption.Encrypt(emergencyPhone) : null,
            encryption.ComputeBlindIndex(tenantId, normalizedPhone),
            normalizedEmail == null ? null : encryption.ComputeBlindIndex(tenantId, normalizedEmail));
    }

    private Result<PatientResponse>? ValidateRequest(SavePatientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Trim().Length < 2)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "First name must be at least 2 characters.");

        if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Trim().Length < 2)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Last name must be at least 2 characters.");

        if (!request.DateOfBirth.HasValue && !request.Age.HasValue)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Date of birth or age is required.");

        if (request.DateOfBirth.HasValue && request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Date of birth cannot be in the future.");

        if (request.Age.HasValue && (request.Age.Value < 0 || request.Age.Value > 150))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Age must be a whole number between 0 and 150.");

        if (request.Gender is not ((byte)Gender.Male) and not ((byte)Gender.Female) and not ((byte)Gender.Other))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Invalid gender.");

        var phone = PhiEncryptionHelper.NormalizePhone(request.Phone);
        if (phone.Length is < 10 or > 15)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Phone number must be 10–15 digits.");

        if (string.IsNullOrWhiteSpace(request.Address))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Address is required.");

        var emergencyPhone = PhiEncryptionHelper.NormalizePhone(request.EmergencyContactPhone ?? string.Empty);
        if (emergencyPhone.Length is > 0 and (< 10 or > 15))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Emergency contact phone must be 10–15 digits.");

        if (!string.IsNullOrWhiteSpace(request.EmergencyContactName) && request.EmergencyContactName.Trim().Length < 2)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Emergency contact name must be at least 2 characters.");

        if (!string.IsNullOrWhiteSpace(request.EmergencyContactName) && emergencyPhone.Length == 0)
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Emergency contact phone is required when name is provided.");

        if (emergencyPhone.Length > 0 && string.IsNullOrWhiteSpace(request.EmergencyContactName))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Emergency contact name is required when phone is provided.");

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@'))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Invalid email format.");

        if (request.BloodGroup.HasValue && !Enum.IsDefined(typeof(BloodGroup), request.BloodGroup.Value))
            return Result<PatientResponse>.Fail(ErrorCode.Validation, "Invalid blood group.");

        return null;
    }

    private PatientListItem MapListItem(PatientRow row) => new(
        row.PatientId,
        row.PatientCode,
        row.FirstName,
        row.LastName,
        encryption.Decrypt(row.PhoneCipher),
        FormatGender(row.Gender),
        row.Gender,
        row.PatientStatus == (byte)PatientStatus.Active ? "Active" : "Inactive",
        row.PatientStatus,
        row.CreatedAt);

    private PatientResponse MapDetail(PatientRow row) => new(
        row.PatientId,
        row.PatientCode,
        row.FirstName,
        row.LastName,
        row.DateOfBirth.HasValue ? DateOnly.FromDateTime(row.DateOfBirth.Value) : null,
        row.Age,
        FormatGender(row.Gender),
        row.Gender,
        encryption.Decrypt(row.PhoneCipher),
        row.EmailCipher == null ? null : encryption.Decrypt(row.EmailCipher),
        encryption.Decrypt(row.AddressCipher),
        row.EmergencyNameCipher == null ? null : encryption.Decrypt(row.EmergencyNameCipher),
        row.EmergencyPhoneCipher == null ? null : encryption.Decrypt(row.EmergencyPhoneCipher),
        row.BloodGroup.HasValue ? FormatBloodGroup((BloodGroup)row.BloodGroup.Value) : null,
        row.BloodGroup,
        row.ReferredBy,
        row.PatientStatus == (byte)PatientStatus.Active ? "Active" : "Inactive",
        row.PatientStatus,
        row.CreatedAt);

    private static string FormatGender(Gender gender) => gender switch
    {
        Gender.Male => "Male",
        Gender.Female => "Female",
        Gender.Other => "Other",
        _ => "Unknown"
    };

    private static string FormatGender(byte gender) => FormatGender((Gender)gender);

    private static string FormatBloodGroup(BloodGroup group) => group switch
    {
        BloodGroup.APositive => "A+",
        BloodGroup.ANegative => "A-",
        BloodGroup.BPositive => "B+",
        BloodGroup.BNegative => "B-",
        BloodGroup.ABPositive => "AB+",
        BloodGroup.ABNegative => "AB-",
        BloodGroup.OPositive => "O+",
        BloodGroup.ONegative => "O-",
        _ => "Unknown"
    };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
