namespace WebApi.Features.Payments.Infrastructure;

public class PaymentStatusRow
{
    public Guid PaymentId { get; set; }
    public byte PaymentStatus { get; set; }
    public decimal? AmountPaid { get; set; }
    public DateTime? CollectionDateTime { get; set; }
    public string? ReceiptNumber { get; set; }
}

public class PaymentListRow
{
    public Guid PaymentId { get; set; }
    public Guid VisitId { get; set; }
    public string VisitCode { get; set; } = string.Empty;
    public DateTime VisitDateTime { get; set; }
    public byte VisitStatus { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public byte PaymentLineType { get; set; }
    public decimal FeeAmount { get; set; }
    public byte PaymentStatus { get; set; }
    public decimal? AmountPaid { get; set; }
    public DateTime? CollectionDateTime { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal BalanceDue { get; set; }
    public Guid? CollectedByUserId { get; set; }
    public string? CollectorFirstName { get; set; }
    public string? CollectorLastName { get; set; }
    public int TotalCount { get; set; }
}

public class AddCollectionResultRow
{
    public Guid PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal BalanceDue { get; set; }
}

public class PaymentReceiptRow
{
    public Guid PaymentId { get; set; }
    public string? ReceiptNumber { get; set; }
    public decimal? AmountPaid { get; set; }
    public byte? PaymentMethod { get; set; }
    public DateTime? CollectionDateTime { get; set; }
    public string? Notes { get; set; }
    public string? CollectorFirstName { get; set; }
    public string? CollectorLastName { get; set; }
    public Guid VisitId { get; set; }
    public string VisitCode { get; set; } = string.Empty;
    public DateTime VisitDateTime { get; set; }
    public decimal? ConsultationFee { get; set; }
    public decimal? ProcedureCharge { get; set; }
    public decimal? Discount { get; set; }
    public decimal AddonCharges { get; set; }
    public decimal? TotalDue { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public int? PatientAge { get; set; }
    public byte PatientGender { get; set; }
    public byte[] PhoneCipher { get; set; } = [];
    public byte[] AddressCipher { get; set; } = [];
    public string? DoctorFirstName { get; set; }
    public string? DoctorLastName { get; set; }
    public string? DoctorDesignation { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalAddress { get; set; } = string.Empty;
    public string HospitalPhone { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? ReceiptHeaderText { get; set; }
    public string? ReceiptFooterText { get; set; }
    public string? GstTaxNumber { get; set; }
    public Guid? DocumentTemplateId { get; set; }
    public string? TemplateBodyHtml { get; set; }
}

public class PaymentReceiptAddonRow
{
    public string AddonName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public interface IPaymentsRepository
{
    Task<(IEnumerable<PaymentListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? patientCode,
        bool openVisitsOnly,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);

    Task<AddCollectionResultRow?> AddCollectionAsync(
        Guid tenantId,
        Guid visitId,
        decimal amount,
        Guid collectedByUserId,
        string? notes,
        byte? paymentMethod,
        Guid actorId,
        CancellationToken ct);

    Task VoidCollectionAsync(
        Guid tenantId,
        Guid paymentId,
        string? reason,
        Guid actorId,
        CancellationToken ct);

    Task<int> CollectVisitPendingAsync(
        Guid tenantId,
        Guid visitId,
        Guid? collectedByUserId,
        Guid actorId,
        CancellationToken ct);

    Task<(PaymentReceiptRow? Receipt, IReadOnlyList<PaymentReceiptAddonRow> Addons)> GetReceiptAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken ct);
}
