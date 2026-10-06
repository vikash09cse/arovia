using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Admissions.Infrastructure;

namespace WebApi.Features.Admissions;

public class AdmissionsService(
    IAdmissionsRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<Result<AdmissionListResponse>> GetListAsync(
        int page,
        int pageSize,
        byte? admissionStatus,
        Guid? patientId,
        string? admissionCode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetListAsync(
            tenantId, page, pageSize, admissionStatus, patientId,
            string.IsNullOrWhiteSpace(admissionCode) ? null : admissionCode.Trim(),
            dateFrom, dateTo, ct);

        var mapped = items.Select(r => new AdmissionListItemResponse(
            r.AdmissionId,
            r.AdmissionCode,
            r.AdmittedAt,
            r.DischargedAt,
            r.Ward,
            r.Bed,
            r.RoomClass,
            FormatStatus(r.AdmissionStatus),
            r.AdmissionStatus,
            r.EstimatedAmount,
            r.DepartmentId,
            r.DepartmentName,
            r.PatientId,
            r.PatientCode,
            r.PatientFirstName,
            r.PatientLastName,
            $"{r.PatientFirstName} {r.PatientLastName}".Trim(),
            r.AttendingDoctorId,
            $"{r.DoctorFirstName} {r.DoctorLastName}".Trim()));

        return Result<AdmissionListResponse>.Ok(new AdmissionListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<AdmissionResponse>> GetByIdAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (admission, charges, payments) = await repository.GetByIdAsync(tenantId, admissionId, ct);
        if (admission == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.NotFound, "Admission not found.");

        return Result<AdmissionResponse>.Ok(MapDetail(admission, charges, payments));
    }

    public async Task<Result<AdmissionResponse>> CreateAsync(CreateAdmissionRequest request, CancellationToken ct)
    {
        var validation = ValidateCreate(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;

        var (activeItems, activeCount) = await repository.GetListAsync(
            tenantId, 1, 1, (byte)AdmissionStatus.Admitted, request.PatientId,
            null, null, null, ct);
        if (activeCount > 0 || activeItems.Any())
        {
            return Result<AdmissionResponse>.Fail(
                ErrorCode.AlreadyExists,
                "Patient is already admitted. Discharge the current stay before admitting again.");
        }

        var bed = string.IsNullOrWhiteSpace(request.Bed) ? null : request.Bed.Trim();
        Guid id;
        try
        {
            id = await repository.SaveAsync(
                tenantId,
                request.PatientId,
                request.DepartmentId,
                request.AttendingDoctorId,
                request.Ward.Trim(),
                bed,
                request.RoomClass.Trim(),
                request.EstimatedAmount,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                request.FromVisitId,
                request.DepositAmount is > 0 ? request.DepositAmount : null,
                request.DepositAmount is > 0 ? request.PaymentMethod : null,
                request.DepositAmount is > 0 ? request.CollectedByUserId : null,
                GetUserId(),
                ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (
            ex.Number is 50409 or 2601 or 2627
            || ex.Message.Contains("already admitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result<AdmissionResponse>.Fail(
                ErrorCode.AlreadyExists,
                "Patient is already admitted. Discharge the current stay before admitting again.");
        }

        var created = await GetByIdAsync(id, ct);
        if (!created.Success || created.Data == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.NotFound, "Admission was created but could not be loaded.");

        return Result<AdmissionResponse>.Ok(created.Data, "Patient admitted successfully.");
    }

    public async Task<Result<AdmissionResponse>> AddChargeAsync(
        Guid admissionId, AddAdmissionChargeRequest request, CancellationToken ct)
    {
        if (request.ChargeCategory is < 1 or > 6)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid charge category.");
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 250)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Charge description is required (max 250).");
        if (request.Amount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid charge amount.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.AddChargeAsync(
            tenantId, admissionId, request.ChargeCategory,
            request.Description.Trim(), request.Amount, GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> ApplyDiscountAsync(
        Guid admissionId, ApplyAdmissionDiscountRequest request, CancellationToken ct)
    {
        if (request.DiscountAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid discount amount.");
        if (request.DiscountAmount > 0 && string.IsNullOrWhiteSpace(request.DiscountReason))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Discount reason is required.");
        if (request.DiscountReason?.Length > 500)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Discount reason cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.ApplyDiscountAsync(
            tenantId, admissionId, request.DiscountAmount,
            request.DiscountAmount > 0 ? request.DiscountReason!.Trim() : null,
            GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> AddPaymentAsync(
        Guid admissionId, AddAdmissionPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount is <= 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Amount must be greater than zero.");
        if (request.CollectedByUserId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Collector is required.");
        if (request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
            and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment method.");
        if (request.PaymentKind is not ((byte)AdmissionPaymentKind.Deposit)
            and not ((byte)AdmissionPaymentKind.Partial)
            and not ((byte)AdmissionPaymentKind.Final))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment kind.");
        if (request.Notes?.Length > 500)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.AddPaymentAsync(
            tenantId, admissionId, request.Amount, request.PaymentMethod, request.PaymentKind,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            request.CollectedByUserId, GetUserId(), ct);

        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<AdmissionResponse>> DischargeAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<AdmissionResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.DischargeAsync(tenantId, admissionId, GetUserId(), ct);
        return await GetByIdAsync(admissionId, ct);
    }

    public async Task<Result<bool>> DeleteAsync(Guid admissionId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.DeleteAsync(tenantId, admissionId, GetUserId(), ct);
        return Result<bool>.Ok(true, "Admission deleted.");
    }

    private static Result<AdmissionResponse>? ValidateCreate(CreateAdmissionRequest request)
    {
        if (request.PatientId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Patient is required.");
        if (request.DepartmentId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Department is required.");
        if (request.AttendingDoctorId == Guid.Empty)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Attending doctor is required.");
        if (string.IsNullOrWhiteSpace(request.Ward) || request.Ward.Trim().Length > 100)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Ward is required (max 100).");
        if (request.Bed != null && request.Bed.Trim().Length > 50)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Bed cannot exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(request.RoomClass) || request.RoomClass.Trim().Length > 50)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Room class is required (max 50).");
        if (request.EstimatedAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid estimated amount.");
        if (request.Notes?.Length > 1000)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 1000 characters.");
        if (request.DepositAmount is < 0 or > 999999.99m)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid deposit amount.");
        if (request.DepositAmount > 0 && request.CollectedByUserId == null)
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Collector is required for deposit.");
        if (request.DepositAmount > 0
            && request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
                and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AdmissionResponse>.Fail(ErrorCode.Validation, "Invalid payment method for deposit.");
        return null;
    }

    private static AdmissionResponse MapDetail(
        AdmissionDetailRow a,
        IEnumerable<AdmissionChargeRow> charges,
        IEnumerable<AdmissionPaymentRow> payments)
    {
        return new AdmissionResponse(
            a.AdmissionId,
            a.AdmissionCode,
            a.AdmittedAt,
            a.DischargedAt,
            a.Ward,
            a.Bed,
            a.RoomClass,
            FormatStatus(a.AdmissionStatus),
            a.AdmissionStatus,
            a.EstimatedAmount,
            a.Notes,
            a.DiscountAmount,
            a.DiscountReason,
            a.FromVisitId,
            a.FromVisitCode,
            a.DepartmentId,
            a.DepartmentName,
            a.PatientId,
            a.PatientCode,
            a.PatientFirstName,
            a.PatientLastName,
            $"{a.PatientFirstName} {a.PatientLastName}".Trim(),
            a.AttendingDoctorId,
            $"{a.DoctorFirstName} {a.DoctorLastName}".Trim(),
            a.ChargesTotal,
            a.BillTotal,
            a.PaidTotal,
            a.BalanceDue,
            a.CreatedAt,
            charges.Select(c => new AdmissionChargeResponse(
                c.AdmissionChargeId,
                c.ChargeCategory,
                FormatChargeCategory(c.ChargeCategory),
                c.Description,
                c.Amount,
                c.ChargedOn,
                FormatName(c.CreatorFirstName, c.CreatorLastName))),
            payments.Select(p => new AdmissionPaymentResponse(
                p.AdmissionPaymentId,
                p.Amount,
                p.PaymentMethod,
                FormatPaymentMethod(p.PaymentMethod),
                p.PaymentKind,
                FormatPaymentKind(p.PaymentKind),
                p.ReceiptNumber,
                p.Notes,
                p.CollectionDateTime,
                p.CollectedByUserId,
                FormatName(p.CollectorFirstName, p.CollectorLastName))));
    }

    private static string FormatStatus(byte code) => code switch
    {
        (byte)AdmissionStatus.Admitted => "Admitted",
        (byte)AdmissionStatus.Discharged => "Discharged",
        (byte)AdmissionStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    private static string FormatChargeCategory(byte code) => code switch
    {
        (byte)AdmissionChargeCategory.Room => "Room",
        (byte)AdmissionChargeCategory.Procedure => "Procedure",
        (byte)AdmissionChargeCategory.Lab => "Lab",
        (byte)AdmissionChargeCategory.Pharmacy => "Pharmacy",
        (byte)AdmissionChargeCategory.Doctor => "Doctor",
        (byte)AdmissionChargeCategory.Other => "Other",
        _ => "Other"
    };

    private static string FormatPaymentKind(byte code) => code switch
    {
        (byte)AdmissionPaymentKind.Deposit => "Deposit",
        (byte)AdmissionPaymentKind.Partial => "Partial",
        (byte)AdmissionPaymentKind.Final => "Final",
        _ => "Unknown"
    };

    private static string FormatPaymentMethod(byte code) => code switch
    {
        (byte)PaymentMethod.Cash => "Cash",
        (byte)PaymentMethod.Upi => "UPI",
        (byte)PaymentMethod.BankAccount => "Bank Account",
        (byte)PaymentMethod.Cheque => "Cheque",
        _ => "Unknown"
    };

    private static string? FormatName(string? first, string? last)
    {
        var name = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
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
