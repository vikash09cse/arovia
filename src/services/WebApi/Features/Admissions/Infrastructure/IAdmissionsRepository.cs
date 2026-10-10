namespace WebApi.Features.Admissions.Infrastructure;

public class AdmissionListRow
{
    public Guid AdmissionId { get; set; }
    public string AdmissionCode { get; set; } = string.Empty;
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string? Bed { get; set; }
    public string RoomClass { get; set; } = string.Empty;
    public byte AdmissionStatus { get; set; }
    public decimal EstimatedAmount { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public Guid AttendingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
}

public class AdmissionDetailRow
{
    public Guid AdmissionId { get; set; }
    public string AdmissionCode { get; set; } = string.Empty;
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string? Bed { get; set; }
    public string RoomClass { get; set; } = string.Empty;
    public byte AdmissionStatus { get; set; }
    public decimal EstimatedAmount { get; set; }
    public string? Notes { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? FromVisitId { get; set; }
    public string? FromVisitCode { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public Guid AttendingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public decimal ChargesTotal { get; set; }
    public decimal BillTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdmissionFinalInvoiceHeaderRow
{
    public Guid AdmissionId { get; set; }
    public string AdmissionCode { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime AdmittedAt { get; set; }
    public DateTime? DischargedAt { get; set; }
    public byte AdmissionStatus { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public string Ward { get; set; } = string.Empty;
    public string? Bed { get; set; }
    public string RoomClass { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public byte PatientGender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public byte[]? PhoneCipher { get; set; }
    public byte[]? AddressCipher { get; set; }
    public Guid AttendingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public string? DoctorDesignation { get; set; }
    public decimal ChargesTotal { get; set; }
    public decimal BillTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal BalanceDue { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalAddress { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ReceiptHeaderText { get; set; }
    public string? ReceiptFooterText { get; set; }
    public Guid? DocumentTemplateId { get; set; }
    public string? TemplateBodyHtml { get; set; }
    public DateTime? ProcedureChargedOn { get; set; }
}

public class AdmissionFinalInvoiceChargeRow
{
    public Guid AdmissionChargeId { get; set; }
    public byte ChargeCategory { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargedOn { get; set; }
}

public class AdmissionFinalInvoicePaymentRow
{
    public Guid AdmissionPaymentId { get; set; }
    public decimal Amount { get; set; }
    public byte PaymentMethod { get; set; }
    public byte PaymentKind { get; set; }
    public string? ReceiptNumber { get; set; }
    public DateTime CollectionDateTime { get; set; }
}

public class AdmissionChargeRow
{
    public Guid AdmissionChargeId { get; set; }
    public byte ChargeCategory { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargedOn { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatorFirstName { get; set; }
    public string? CreatorLastName { get; set; }
}

public class AdmissionPaymentRow
{
    public Guid AdmissionPaymentId { get; set; }
    public decimal Amount { get; set; }
    public byte PaymentMethod { get; set; }
    public byte PaymentKind { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CollectionDateTime { get; set; }
    public Guid CollectedByUserId { get; set; }
    public string? CollectorFirstName { get; set; }
    public string? CollectorLastName { get; set; }
}

public interface IAdmissionsRepository
{
    Task<(IEnumerable<AdmissionListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        byte? admissionStatus,
        Guid? patientId,
        string? admissionCode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);

    Task<(AdmissionDetailRow? Admission, IEnumerable<AdmissionChargeRow> Charges, IEnumerable<AdmissionPaymentRow> Payments)> GetByIdAsync(
        Guid tenantId,
        Guid admissionId,
        CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid patientId,
        Guid departmentId,
        Guid attendingDoctorId,
        string ward,
        string? bed,
        string roomClass,
        decimal estimatedAmount,
        string? notes,
        Guid? fromVisitId,
        decimal? depositAmount,
        byte? depositPaymentMethod,
        Guid? collectedBy,
        DateOnly? admissionDate,
        Guid actorId,
        CancellationToken ct);

    Task<Guid> AddChargeAsync(
        Guid tenantId,
        Guid admissionId,
        byte chargeCategory,
        string description,
        decimal amount,
        Guid actorId,
        CancellationToken ct);

    Task ApplyDiscountAsync(
        Guid tenantId,
        Guid admissionId,
        decimal discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct);

    Task<Guid> AddPaymentAsync(
        Guid tenantId,
        Guid admissionId,
        decimal amount,
        byte paymentMethod,
        byte paymentKind,
        string? notes,
        Guid collectedBy,
        Guid actorId,
        CancellationToken ct);

    Task DischargeAsync(Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct);

    Task<string> EnsureInvoiceNumberAsync(Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct);

    Task<(AdmissionFinalInvoiceHeaderRow? Header, IReadOnlyList<AdmissionFinalInvoiceChargeRow> Charges, IReadOnlyList<AdmissionFinalInvoicePaymentRow> Payments)> GetFinalInvoiceDataAsync(
        Guid tenantId,
        Guid admissionId,
        CancellationToken ct);
}
