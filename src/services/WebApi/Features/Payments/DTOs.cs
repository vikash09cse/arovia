namespace WebApi.Features.Payments;

public record AddPaymentRequest(
    decimal Amount,
    Guid CollectedByUserId,
    string? Notes = null,
    byte? PaymentMethod = null);

public record VoidPaymentRequest(string? Reason = null);

public record AddPaymentResponse(
    Guid Id,
    string ReceiptNumber,
    decimal Amount,
    decimal TotalDue,
    decimal TotalCollected,
    decimal BalanceDue);

public record PaymentStatusResponse(
    Guid Id,
    string Status,
    byte StatusCode,
    decimal? AmountPaid,
    DateTime? CollectionDateTime,
    string? ReceiptNumber);

public record PaymentListItemResponse(
    Guid Id,
    Guid VisitId,
    string VisitCode,
    DateTime VisitDateTime,
    byte VisitStatusCode,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    string PatientFullName,
    decimal Amount,
    string? ReceiptNumber,
    DateTime? CollectionDateTime,
    string? CollectedByName,
    string? Notes,
    decimal TotalDue,
    decimal TotalCollected,
    decimal BalanceDue,
    DateTime CreatedAt);

public record PaymentListResponse(
    IEnumerable<PaymentListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record CollectVisitPendingRequest(Guid? CollectedByUserId = null);
