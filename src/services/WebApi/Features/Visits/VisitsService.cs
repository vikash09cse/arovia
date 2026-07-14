using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using SharedKernel.Utilities.Helpers;
using WebApi.Features.LabTests;
using WebApi.Features.VisitAddons;
using WebApi.Features.Visits.Infrastructure;

namespace WebApi.Features.Visits;

public class VisitsService(
    IVisitsRepository repository,
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

    private static string FormatVisitType(byte code) => ((VisitType)code) switch
    {
        VisitType.Opd => "OPD",
        VisitType.FollowUp => "Follow-up",
        VisitType.PreOp => "Pre-op",
        VisitType.Surgery => "Surgery",
        _ => "Unknown"
    };

    private static string FormatFeeStatus(byte code) => ((VisitFeeStatus)code) switch
    {
        VisitFeeStatus.Charged => "Charged",
        VisitFeeStatus.Free => "Free",
        _ => "Unknown"
    };

    private static string FormatVisitStatus(byte code) => ((VisitStatus)code) switch
    {
        VisitStatus.Active => "Active",
        VisitStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    private static string FormatPaymentLineType(byte code) => ((PaymentLineType)code) switch
    {
        PaymentLineType.Consultation => "Consultation",
        PaymentLineType.Procedure => "Procedure",
        _ => "Unknown"
    };

    private static string FormatPaymentStatus(byte code) => ((PaymentStatus)code) switch
    {
        PaymentStatus.Pending => "Pending",
        PaymentStatus.Paid => "Paid",
        PaymentStatus.Refunded => "Refunded",
        _ => "Unknown"
    };

    private static string FormatAggregatedPaymentStatus(byte code) => ((AggregatedPaymentStatus)code) switch
    {
        AggregatedPaymentStatus.None => "—",
        AggregatedPaymentStatus.Pending => "Pending",
        AggregatedPaymentStatus.Paid => "Paid",
        AggregatedPaymentStatus.Partial => "Partial",
        _ => "—"
    };

    public async Task<Result<VisitListResponse>> GetVisitsAsync(
        int page, int pageSize, Guid? patientId, string? patientCode, string? visitCode, string? phone, Guid? consultingDoctorId,
        byte? visitType, byte? feeStatus, byte? visitStatus, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<VisitListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;

        byte[]? phoneBlindIndex = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var normalized = PhiEncryptionHelper.NormalizePhone(phone);
            if (normalized.Length is < 10 or > 15)
                return Result<VisitListResponse>.Fail(ErrorCode.Validation, "Phone search must be 10–15 digits.");
            phoneBlindIndex = encryption.ComputeBlindIndex(tenantId, normalized);
        }

        var (items, total) = await repository.GetListAsync(
            tenantId, page, pageSize, patientId,
            string.IsNullOrWhiteSpace(patientCode) ? null : patientCode.Trim(),
            string.IsNullOrWhiteSpace(visitCode) ? null : visitCode.Trim(),
            phoneBlindIndex,
            consultingDoctorId, visitType, feeStatus, visitStatus, dateFrom, dateTo, ct);

        var mapped = items.Select(MapListItem);
        return Result<VisitListResponse>.Ok(new VisitListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<VisitResponse>> GetByIdAsync(Guid visitId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<VisitResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (visit, payments, labAgencies, addons) = await repository.GetByIdAsync(tenantId, visitId, ct);
        if (visit == null)
            return Result<VisitResponse>.Fail(ErrorCode.NotFound, "Visit not found.");

        return Result<VisitResponse>.Ok(MapDetail(visit, payments, labAgencies, addons));
    }

    public async Task<Result<FeePreviewResponse>> GetFeePreviewAsync(Guid patientId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<FeePreviewResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetFeePreviewAsync(tenantId, patientId, ct);
        if (row == null)
            return Result<FeePreviewResponse>.Fail(ErrorCode.NotFound, "Unable to compute fee preview.");

        return Result<FeePreviewResponse>.Ok(new FeePreviewResponse(
            FormatFeeStatus(row.ProposedFeeStatus),
            row.ProposedFeeStatus,
            row.ProposedFeeAmount,
            row.TenantVisitFeeAmount,
            row.FreeVisitWindowDays,
            row.LastChargedVisitDateTime,
            row.DaysSinceLastCharged));
    }

    public async Task<Result<IEnumerable<DoctorLookupItem>>> GetDoctorsAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<DoctorLookupItem>>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var rows = await repository.GetActiveDoctorsAsync(tenantId, ct);
        var mapped = rows.Select(d => new DoctorLookupItem(
            d.UserId, d.FirstName, d.LastName, $"{d.FirstName} {d.LastName}".Trim()));
        return Result<IEnumerable<DoctorLookupItem>>.Ok(mapped);
    }

    public async Task<Result<IEnumerable<DoctorLookupItem>>> GetPaymentCollectorsAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<DoctorLookupItem>>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var rows = await repository.GetPaymentCollectorsAsync(tenantId, ct);
        var mapped = rows.Select(d => new DoctorLookupItem(
            d.UserId, d.FirstName, d.LastName, $"{d.FirstName} {d.LastName}".Trim()));
        return Result<IEnumerable<DoctorLookupItem>>.Ok(mapped);
    }

    public async Task<Result<VisitResponse>> CreateAsync(CreateVisitRequest request, CancellationToken ct)
    {
        var validation = ValidateCreateRequest(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<VisitResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var visitId = await repository.SaveAsync(
            tenantId,
            request.PatientId,
            request.ConsultingDoctorId,
            request.VisitType,
            string.IsNullOrWhiteSpace(request.Purpose) ? null : request.Purpose.Trim(),
            string.IsNullOrWhiteSpace(request.VisitNotes) ? null : request.VisitNotes.Trim(),
            request.ProcedureChargeAmount,
            request.ScheduledSurgeryDate,
            request.FeeStatus,
            request.ConsultationFeeAmount,
            string.IsNullOrWhiteSpace(request.FeeNote) ? null : request.FeeNote.Trim(),
            request.InitialCollectionAmount,
            request.CollectedByUserId,
            request.AddonIds,
            request.DiscountAmount,
            string.IsNullOrWhiteSpace(request.DiscountReason) ? null : request.DiscountReason.Trim(),
            GetUserId(),
            ct);

        var created = await GetByIdAsync(visitId, ct);
        if (!created.Success || created.Data == null)
            return Result<VisitResponse>.Fail(ErrorCode.NotFound, "Visit was created but could not be loaded.");

        return Result<VisitResponse>.Ok(created.Data, "Visit recorded successfully.");
    }

    public async Task<Result<VisitResponse>> UpdateNotesAsync(Guid visitId, UpdateVisitNotesRequest request, CancellationToken ct)
    {
        if (request.VisitNotes.Length > 1000)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Visit notes cannot exceed 1000 characters.");

        var tenantError = RequireTenantContext<VisitResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.UpdateNotesAsync(tenantId, visitId, request.VisitNotes.Trim(), GetUserId(), ct);
        return await GetByIdAsync(visitId, ct);
    }

    public async Task<Result<VisitResponse>> OverrideFeeAsync(Guid visitId, FeeOverrideRequest request, CancellationToken ct)
    {
        if (request.FeeStatus is not ((byte)VisitFeeStatus.Charged) and not ((byte)VisitFeeStatus.Free))
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid fee status.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Fee override reason is required.");

        var tenantError = RequireTenantContext<VisitResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.OverrideFeeAsync(tenantId, visitId, request.FeeStatus, request.Reason.Trim(), GetUserId(), ct);
        return await GetByIdAsync(visitId, ct);
    }

    public async Task<Result<VisitResponse>> ApplyDiscountAsync(Guid visitId, ApplyDiscountRequest request, CancellationToken ct)
    {
        var validation = ValidateDiscountRequest(request.DiscountAmount, request.DiscountReason);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<VisitResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (visitRow, _, _, _) = await repository.GetByIdAsync(tenantId, visitId, ct);
        if (visitRow == null)
            return Result<VisitResponse>.Fail(ErrorCode.NotFound, "Visit not found.");

        var sameDayError = ValidateStaffSameDayFeeChange<VisitResponse>(visitRow.VisitDateTime);
        if (sameDayError != null) return sameDayError;

        var reason = request.DiscountAmount > 0
            ? request.DiscountReason!.Trim()
            : null;

        await repository.ApplyDiscountAsync(
            tenantId, visitId, request.DiscountAmount, reason, GetUserId(), ct);

        return await GetByIdAsync(visitId, ct);
    }

    public async Task<Result<bool>> CancelAsync(Guid visitId, CancelVisitRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        if (request.Reason?.Length > 500)
            return Result<bool>.Fail(ErrorCode.Validation, "Cancellation reason cannot exceed 500 characters.");

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        await repository.CancelAsync(tenantId, visitId, reason, GetUserId(), ct);
        return Result<bool>.Ok(true, "Visit cancelled.");
    }

    public async Task<Result<VisitSummaryResponse>> GetPatientSummaryAsync(Guid patientId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<VisitSummaryResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetPatientSummaryAsync(tenantId, patientId, ct);
        if (row == null)
            return Result<VisitSummaryResponse>.Ok(new VisitSummaryResponse(0, 0, 0, null));

        DateOnly? surgeryDate = row.UpcomingScheduledSurgeryDate.HasValue
            ? DateOnly.FromDateTime(row.UpcomingScheduledSurgeryDate.Value)
            : null;

        return Result<VisitSummaryResponse>.Ok(new VisitSummaryResponse(
            row.TotalVisits, row.TotalCharged, row.TotalFree, surgeryDate));
    }

    private static Result<VisitResponse>? ValidateCreateRequest(CreateVisitRequest request)
    {
        if (request.PatientId == Guid.Empty)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Patient is required.");

        if (request.ConsultingDoctorId == Guid.Empty)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Consulting doctor is required.");

        if (request.VisitType is not ((byte)VisitType.Opd) and not ((byte)VisitType.FollowUp)
            and not ((byte)VisitType.PreOp) and not ((byte)VisitType.Surgery))
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid visit type.");

        if (!string.IsNullOrWhiteSpace(request.Purpose) && request.Purpose.Trim().Length > 300)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Purpose cannot exceed 300 characters.");

        if (request.VisitNotes?.Length > 1000)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Visit notes cannot exceed 1000 characters.");

        if (request.ProcedureChargeAmount is < 0 or > 999999.99m)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid procedure charge amount.");

        if (request.FeeStatus is not ((byte)VisitFeeStatus.Charged) and not ((byte)VisitFeeStatus.Free))
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid consultation fee status.");

        if (request.FeeStatus == (byte)VisitFeeStatus.Charged)
        {
            if (request.ConsultationFeeAmount is null or < 0 or > 999999.99m)
                return Result<VisitResponse>.Fail(ErrorCode.Validation, "Consultation fee amount is required when charged.");
        }

        if (request.FeeNote?.Length > 500)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Fee note cannot exceed 500 characters.");

        if (request.InitialCollectionAmount is < 0 or > 999999.99m)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid initial collection amount.");

        if (request.InitialCollectionAmount > 0 && request.CollectedByUserId == null)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Payment collector is required when collecting at visit create.");

        if (request.AddonIds != null)
        {
            if (request.AddonIds.Any(id => id == Guid.Empty))
                return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid visit addon selection.");

            if (request.AddonIds.Count != request.AddonIds.Distinct().Count())
                return Result<VisitResponse>.Fail(ErrorCode.Validation, "Duplicate visit addons are not allowed.");
        }

        var discountValidation = ValidateDiscountRequest(request.DiscountAmount ?? 0, request.DiscountReason);
        if (discountValidation != null) return discountValidation;

        return null;
    }

    private static Result<VisitResponse>? ValidateDiscountRequest(decimal discountAmount, string? discountReason)
    {
        if (discountAmount < 0 || discountAmount > 999999.99m)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Invalid discount amount.");

        if (discountAmount > 0 && string.IsNullOrWhiteSpace(discountReason))
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Discount reason is required when applying a discount.");

        if (discountReason?.Length > 500)
            return Result<VisitResponse>.Fail(ErrorCode.Validation, "Discount reason cannot exceed 500 characters.");

        return null;
    }

    private VisitListItem MapListItem(VisitListRow row) => new(
        row.VisitId,
        row.VisitCode,
        row.VisitDateTime,
        FormatVisitType(row.VisitType),
        row.VisitType,
        row.Purpose,
        FormatFeeStatus(row.FeeStatus),
        row.FeeStatus,
        row.FeeAmount,
        row.ProcedureChargeAmount,
        row.TotalChargeAmount,
        FormatVisitStatus(row.VisitStatus),
        row.VisitStatus,
        row.ScheduledSurgeryDate.HasValue ? DateOnly.FromDateTime(row.ScheduledSurgeryDate.Value) : null,
        row.PatientId,
        row.PatientCode,
        row.PatientFirstName,
        row.PatientLastName,
        row.ConsultingDoctorId,
        $"{row.DoctorFirstName} {row.DoctorLastName}".Trim(),
        FormatAggregatedPaymentStatus(row.AggregatedPaymentStatus),
        row.AggregatedPaymentStatus);

    private VisitResponse MapDetail(
        VisitDetailRow visit,
        IEnumerable<PaymentLineRow> payments,
        IEnumerable<VisitLabAgencyRow> labAgencies,
        IEnumerable<VisitAddonLineRow> addons)
    {
        var paymentList = payments.ToList();
        var labAgencyList = labAgencies.ToList();
        var addonList = addons.ToList();
        var aggregated = ComputeAggregatedStatus(visit.TotalDue, visit.TotalCollected);
        var discountAmount = visit.DiscountAmount ?? 0;
        var grossSubtotal = visit.TotalDue + discountAmount;

        return new VisitResponse(
            visit.VisitId,
            visit.VisitCode,
            visit.VisitDateTime,
            FormatVisitType(visit.VisitType),
            visit.VisitType,
            visit.Purpose,
            visit.VisitNotes,
            visit.ScheduledSurgeryDate.HasValue ? DateOnly.FromDateTime(visit.ScheduledSurgeryDate.Value) : null,
            FormatFeeStatus(visit.FeeStatus),
            visit.FeeStatus,
            visit.FeeAmount,
            visit.ProcedureChargeAmount,
            visit.TotalChargeAmount,
            visit.DiscountAmount,
            visit.DiscountReason,
            grossSubtotal,
            FormatVisitStatus(visit.VisitStatus),
            visit.VisitStatus,
            visit.IsFeeOverridden,
            visit.FeeOverrideReason,
            visit.CancellationReason,
            visit.FreeVisitWindowDaysSnapshot,
            visit.DaysSinceLastCharged,
            visit.LastChargedVisitDateTime,
            visit.CreatedAt,
            visit.PatientId,
            visit.PatientCode,
            visit.PatientFirstName,
            visit.PatientLastName,
            visit.ConsultingDoctorId,
            visit.DoctorFirstName,
            visit.DoctorLastName,
            $"{visit.DoctorFirstName} {visit.DoctorLastName}".Trim(),
            visit.TotalDue,
            visit.TotalCollected,
            visit.BalanceDue,
            FormatAggregatedPaymentStatus(aggregated),
            aggregated,
            paymentList.Select(p =>
            {
                var collectorName = $"{p.CollectorFirstName} {p.CollectorLastName}".Trim();
                if (string.IsNullOrWhiteSpace(collectorName))
                    collectorName = null;

                return new PaymentCollectionResponse(
                    p.PaymentId,
                    p.AmountPaid ?? p.FeeAmount,
                    p.ReceiptNumber,
                    p.CollectionDateTime,
                    p.CollectedByUserId,
                    collectorName,
                    p.Notes);
            }),
            labAgencyList.Select(la =>
            {
                var assignerName = $"{la.AssignerFirstName} {la.AssignerLastName}".Trim();
                if (string.IsNullOrWhiteSpace(assignerName))
                    assignerName = null;

                return new VisitLabAgencyResponse(
                    la.VisitLabAgencyId,
                    la.LabAgencyId,
                    la.AgencyName,
                    la.AssignedAt,
                    la.AssignedByUserId,
                    assignerName,
                    la.TestName,
                    la.Notes);
            }),
            addonList.Select(a => new VisitAddonLineResponse(
                a.VisitAddonLineId,
                a.VisitAddonId,
                a.AddonName,
                a.Amount,
                a.CreatedAt)),
            CanEditFeesOnVisit(visit.VisitDateTime));
    }

    private bool CanEditFeesOnVisit(DateTime visitDateTimeUtc) =>
        httpContextAccessor.GetTenantContext().Role switch
        {
            RoleNames.TenantSuperAdmin => true,
            RoleNames.Staff => IsSameHospitalCalendarDay(visitDateTimeUtc, DateTime.UtcNow),
            _ => false
        };

    private Result<T>? ValidateStaffSameDayFeeChange<T>(DateTime visitDateTimeUtc)
    {
        if (httpContextAccessor.GetTenantContext().Role != RoleNames.Staff)
            return null;

        if (IsSameHospitalCalendarDay(visitDateTimeUtc, DateTime.UtcNow))
            return null;

        return Result<T>.Fail(
            ErrorCode.Validation,
            "Staff can only change fees and prices on the same day as the visit.");
    }

    private static bool IsSameHospitalCalendarDay(DateTime visitUtc, DateTime referenceUtc)
    {
        var tz = GetHospitalTimeZone();
        var visitLocal = TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(visitUtc), tz);
        var referenceLocal = TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(referenceUtc), tz);
        return visitLocal.Date == referenceLocal.Date;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static TimeZoneInfo GetHospitalTimeZone()
    {
        foreach (var id in new[] { "India Standard Time", "Asia/Kolkata" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }

    private static byte ComputeAggregatedStatus(decimal totalDue, decimal totalCollected)
    {
        if (totalDue <= 0)
            return (byte)AggregatedPaymentStatus.None;

        if (totalCollected <= 0)
            return (byte)AggregatedPaymentStatus.Pending;

        if (totalCollected >= totalDue)
            return (byte)AggregatedPaymentStatus.Paid;

        return (byte)AggregatedPaymentStatus.Partial;
    }
}
