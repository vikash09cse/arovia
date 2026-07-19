using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Payments.Infrastructure;

namespace WebApi.Features.Payments;

public class PaymentsService(
    IPaymentsRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<Result<PaymentListResponse>> GetListAsync(
        int page,
        int pageSize,
        string? patientCode,
        bool openVisitsOnly,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        var tenantError = RequireTenantContext<PaymentListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var (items, total) = await repository.GetListAsync(
            tenantId,
            page,
            pageSize,
            string.IsNullOrWhiteSpace(patientCode) ? null : patientCode.Trim(),
            openVisitsOnly,
            dateFrom,
            dateTo,
            ct);

        var mapped = items.Select(MapListItem);
        return Result<PaymentListResponse>.Ok(new PaymentListResponse(mapped, total, page, pageSize));
    }

    public async Task<Result<AddPaymentResponse>> AddCollectionAsync(
        Guid visitId, AddPaymentRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Amount must be greater than zero.");

        if (request.CollectedByUserId == Guid.Empty)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Payment collector is required.");

        if (request.Notes?.Length > 500)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Notes cannot exceed 500 characters.");

        if (request.PaymentMethod is not null
            && request.PaymentMethod is not ((byte)PaymentMethod.Cash) and not ((byte)PaymentMethod.Upi)
                and not ((byte)PaymentMethod.BankAccount) and not ((byte)PaymentMethod.Cheque))
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Invalid payment mode.");

        var tenantError = RequireTenantContext<AddPaymentResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var result = await repository.AddCollectionAsync(
            tenantId,
            visitId,
            request.Amount,
            request.CollectedByUserId,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            request.PaymentMethod,
            GetUserId(),
            ct);

        if (result == null)
            return Result<AddPaymentResponse>.Fail(ErrorCode.Validation, "Unable to record payment collection.");

        return Result<AddPaymentResponse>.Ok(new AddPaymentResponse(
            result.PaymentId,
            result.ReceiptNumber,
            result.Amount,
            result.TotalDue,
            result.TotalCollected,
            result.BalanceDue), "Payment recorded.");
    }

    public async Task<Result<bool>> VoidCollectionAsync(
        Guid paymentId, VoidPaymentRequest request, CancellationToken ct)
    {
        if (request.Reason?.Length > 500)
            return Result<bool>.Fail(ErrorCode.Validation, "Reason cannot exceed 500 characters.");

        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        await repository.VoidCollectionAsync(
            tenantId,
            paymentId,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            GetUserId(),
            ct);

        return Result<bool>.Ok(true, "Payment voided.");
    }

    public async Task<Result<int>> CollectVisitPendingAsync(
        Guid visitId, CollectVisitPendingRequest? request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<int>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var count = await repository.CollectVisitPendingAsync(
            tenantId, visitId, request?.CollectedByUserId, GetUserId(), ct);
        return Result<int>.Ok(count, count > 0 ? "Remaining balance collected." : "No balance due to collect.");
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;

    private static PaymentListItemResponse MapListItem(PaymentListRow row)
    {
        var collectorName = $"{row.CollectorFirstName} {row.CollectorLastName}".Trim();
        if (string.IsNullOrWhiteSpace(collectorName))
            collectorName = null;

        return new PaymentListItemResponse(
            row.PaymentId,
            row.VisitId,
            row.VisitCode,
            row.VisitDateTime,
            row.VisitStatus,
            row.PatientCode,
            row.PatientFirstName,
            row.PatientLastName,
            $"{row.PatientFirstName} {row.PatientLastName}".Trim(),
            row.AmountPaid ?? row.FeeAmount,
            row.ReceiptNumber,
            row.CollectionDateTime,
            collectorName,
            row.Notes,
            row.TotalDue,
            row.TotalCollected,
            row.BalanceDue,
            row.CreatedAt);
    }
}
