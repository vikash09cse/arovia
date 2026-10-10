namespace WebApi.Features.Admissions;

public record CreateAdmissionRequest(
    Guid PatientId,
    Guid DepartmentId,
    Guid AttendingDoctorId,
    string Ward,
    string RoomClass,
    string? Bed = null,
    decimal EstimatedAmount = 0,
    string? Notes = null,
    Guid? FromVisitId = null,
    decimal? DepositAmount = null,
    Guid? CollectedByUserId = null,
    byte? PaymentMethod = null,
    DateOnly? AdmissionDate = null);

public record AddAdmissionChargeRequest(
    byte ChargeCategory,
    string Description,
    decimal Amount);

public record ApplyAdmissionDiscountRequest(
    decimal DiscountAmount,
    string? DiscountReason);

public record AddAdmissionPaymentRequest(
    decimal Amount,
    Guid CollectedByUserId,
    byte PaymentMethod,
    byte PaymentKind,
    string? Notes = null);

public record AdmissionChargeResponse(
    Guid Id,
    byte ChargeCategoryCode,
    string ChargeCategory,
    string Description,
    decimal Amount,
    DateTime ChargedOn,
    string? CreatedByName);

public record AdmissionPaymentResponse(
    Guid Id,
    decimal Amount,
    byte PaymentMethodCode,
    string PaymentMethod,
    byte PaymentKindCode,
    string PaymentKind,
    string? ReceiptNumber,
    string? Notes,
    DateTime CollectionDateTime,
    Guid CollectedByUserId,
    string? CollectedByName);

public record AdmissionListItemResponse(
    Guid Id,
    string AdmissionCode,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    string Ward,
    string? Bed,
    string RoomClass,
    string Status,
    byte StatusCode,
    decimal EstimatedAmount,
    Guid DepartmentId,
    string DepartmentName,
    Guid PatientId,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    string PatientFullName,
    Guid AttendingDoctorId,
    string DoctorName);

public record AdmissionListResponse(
    IEnumerable<AdmissionListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record AdmissionResponse(
    Guid Id,
    string AdmissionCode,
    DateTime AdmittedAt,
    DateTime? DischargedAt,
    string Ward,
    string? Bed,
    string RoomClass,
    string Status,
    byte StatusCode,
    decimal EstimatedAmount,
    string? Notes,
    decimal DiscountAmount,
    string? DiscountReason,
    string? InvoiceNumber,
    Guid? FromVisitId,
    string? FromVisitCode,
    Guid DepartmentId,
    string DepartmentName,
    Guid PatientId,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    string PatientFullName,
    Guid AttendingDoctorId,
    string DoctorName,
    decimal ChargesTotal,
    decimal BillTotal,
    decimal PaidTotal,
    decimal BalanceDue,
    DateTime CreatedAt,
    IEnumerable<AdmissionChargeResponse> Charges,
    IEnumerable<AdmissionPaymentResponse> Payments);

public record AdmissionFinalInvoiceResponse(
    Guid AdmissionId,
    string InvoiceNumber,
    string Html);
