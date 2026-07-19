namespace WebApi.Features.Visits.Infrastructure;

public class VisitListRow
{
    public Guid VisitId { get; set; }
    public string VisitCode { get; set; } = string.Empty;
    public DateTime VisitDateTime { get; set; }
    public byte VisitType { get; set; }
    public string? Purpose { get; set; }
    public byte FeeStatus { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? ProcedureChargeAmount { get; set; }
    public decimal? TotalChargeAmount { get; set; }
    public byte VisitStatus { get; set; }
    public DateTime? ScheduledSurgeryDate { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public Guid ConsultingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public byte AggregatedPaymentStatus { get; set; }
    public int TotalCount { get; set; }
}

public class VisitDetailRow
{
    public Guid VisitId { get; set; }
    public string VisitCode { get; set; } = string.Empty;
    public DateTime VisitDateTime { get; set; }
    public byte VisitType { get; set; }
    public string? Purpose { get; set; }
    public string? VisitNotes { get; set; }
    public DateTime? ScheduledSurgeryDate { get; set; }
    public byte FeeStatus { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? ProcedureChargeAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public decimal? TotalChargeAmount { get; set; }
    public byte VisitStatus { get; set; }
    public bool IsFeeOverridden { get; set; }
    public string? FeeOverrideReason { get; set; }
    public string? CancellationReason { get; set; }
    public int FreeVisitWindowDaysSnapshot { get; set; }
    public int? DaysSinceLastCharged { get; set; }
    public DateTime? LastChargedVisitDateTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public Guid ConsultingDoctorId { get; set; }
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;
    public decimal TotalDue { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal BalanceDue { get; set; }
}

public class PaymentLineRow
{
    public Guid PaymentId { get; set; }
    public byte PaymentLineType { get; set; }
    public decimal FeeAmount { get; set; }
    public byte PaymentStatus { get; set; }
    public string? ReceiptNumber { get; set; }
    public decimal? AmountPaid { get; set; }
    public byte? PaymentMethod { get; set; }
    public DateTime? CollectionDateTime { get; set; }
    public string? Notes { get; set; }
    public Guid? CollectedByUserId { get; set; }
    public string? CollectorFirstName { get; set; }
    public string? CollectorLastName { get; set; }
}

public class VisitLabAgencyRow
{
    public Guid VisitLabAgencyId { get; set; }
    public Guid LabAgencyId { get; set; }
    public string AgencyName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public Guid AssignedByUserId { get; set; }
    public string? AssignerFirstName { get; set; }
    public string? AssignerLastName { get; set; }
    public string? TestName { get; set; }
    public string? Notes { get; set; }
}

public class VisitAddonLineRow
{
    public Guid VisitAddonLineId { get; set; }
    public Guid VisitAddonId { get; set; }
    public string AddonName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FeePreviewRow
{
    public byte ProposedFeeStatus { get; set; }
    public decimal? ProposedFeeAmount { get; set; }
    public decimal TenantVisitFeeAmount { get; set; }
    public int FreeVisitWindowDays { get; set; }
    public DateTime? LastChargedVisitDateTime { get; set; }
    public int? DaysSinceLastCharged { get; set; }
}

public class DoctorRow
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class VisitSummaryRow
{
    public int TotalVisits { get; set; }
    public int TotalCharged { get; set; }
    public int TotalFree { get; set; }
    public DateTime? UpcomingScheduledSurgeryDate { get; set; }
}

public interface IVisitsRepository
{
    Task<FeePreviewRow?> GetFeePreviewAsync(Guid tenantId, Guid patientId, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid patientId,
        Guid consultingDoctorId,
        byte visitType,
        string? purpose,
        string? visitNotes,
        decimal? procedureChargeAmount,
        DateOnly? scheduledSurgeryDate,
        byte feeStatus,
        decimal? consultationFeeAmount,
        string? feeNote,
        decimal? initialCollectionAmount,
        Guid? collectedByUserId,
        byte? paymentMethod,
        IReadOnlyList<Guid>? addonIds,
        decimal? discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct);

    Task<(IEnumerable<VisitListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        Guid? patientId,
        string? patientCode,
        string? visitCode,
        byte[]? phoneBlindIndex,
        Guid? consultingDoctorId,
        byte? visitType,
        byte? feeStatus,
        byte? visitStatus,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct);

    Task<(VisitDetailRow? Visit, IEnumerable<PaymentLineRow> PaymentLines, IEnumerable<VisitLabAgencyRow> LabAgencies, IEnumerable<VisitAddonLineRow> Addons)> GetByIdAsync(
        Guid tenantId, Guid visitId, CancellationToken ct);

    Task<VisitSummaryRow?> GetPatientSummaryAsync(Guid tenantId, Guid patientId, CancellationToken ct);

    Task UpdateNotesAsync(Guid tenantId, Guid visitId, string visitNotes, Guid actorId, CancellationToken ct);

    Task OverrideFeeAsync(Guid tenantId, Guid visitId, byte feeStatus, string reason, Guid actorId, CancellationToken ct);

    Task ApplyDiscountAsync(
        Guid tenantId,
        Guid visitId,
        decimal discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid visitId, Guid actorId, CancellationToken ct);

    Task<IEnumerable<DoctorRow>> GetActiveDoctorsAsync(Guid tenantId, CancellationToken ct);

    Task<IEnumerable<DoctorRow>> GetPaymentCollectorsAsync(Guid tenantId, CancellationToken ct);
}
