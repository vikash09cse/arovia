namespace WebApi.Features.Visits;

using WebApi.Features.LabTests;
using WebApi.Features.VisitAddons;

public record CreateVisitRequest(
    Guid PatientId,
    Guid ConsultingDoctorId,
    byte VisitType,
    string Purpose,
    string? VisitNotes,
    decimal? ProcedureChargeAmount,
    DateOnly? ScheduledSurgeryDate,
    byte FeeStatus,
    decimal? ConsultationFeeAmount,
    string? FeeNote,
    decimal? InitialCollectionAmount = null,
    Guid? CollectedByUserId = null,
    IReadOnlyList<Guid>? AddonIds = null,
    decimal? DiscountAmount = null,
    string? DiscountReason = null);

public record UpdateVisitNotesRequest(string VisitNotes);

public record ApplyDiscountRequest(decimal DiscountAmount, string? DiscountReason);

public record FeeOverrideRequest(byte FeeStatus, string Reason);

public record CancelVisitRequest(string? Reason);

public record FeePreviewResponse(
    string ProposedFeeStatus,
    byte ProposedFeeStatusCode,
    decimal? ProposedFeeAmount,
    decimal TenantVisitFeeAmount,
    int FreeVisitWindowDays,
    DateTime? LastChargedVisitDateTime,
    int? DaysSinceLastCharged);

public record DoctorLookupItem(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName);

public record PaymentCollectionResponse(
    Guid Id,
    decimal Amount,
    string? ReceiptNumber,
    DateTime? CollectionDateTime,
    Guid? CollectedByUserId,
    string? CollectedByName,
    string? Notes);

public record VisitResponse(
    Guid Id,
    string VisitCode,
    DateTime VisitDateTime,
    string VisitType,
    byte VisitTypeCode,
    string Purpose,
    string? VisitNotes,
    DateOnly? ScheduledSurgeryDate,
    string FeeStatus,
    byte FeeStatusCode,
    decimal? FeeAmount,
    decimal? ProcedureChargeAmount,
    decimal? TotalChargeAmount,
    decimal? DiscountAmount,
    string? DiscountReason,
    decimal GrossSubtotal,
    string VisitStatus,
    byte VisitStatusCode,
    bool IsFeeOverridden,
    string? FeeOverrideReason,
    string? CancellationReason,
    int FreeVisitWindowDaysSnapshot,
    int? DaysSinceLastCharged,
    DateTime? LastChargedVisitDateTime,
    DateTime CreatedAt,
    Guid PatientId,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    Guid ConsultingDoctorId,
    string DoctorFirstName,
    string DoctorLastName,
    string DoctorFullName,
    decimal TotalDue,
    decimal TotalCollected,
    decimal BalanceDue,
    string AggregatedPaymentStatus,
    byte AggregatedPaymentStatusCode,
    IEnumerable<PaymentCollectionResponse> PaymentCollections,
    IEnumerable<VisitLabAgencyResponse> LabAgencies,
    IEnumerable<VisitAddonLineResponse> Addons);

public record VisitListItem(
    Guid Id,
    string VisitCode,
    DateTime VisitDateTime,
    string VisitType,
    byte VisitTypeCode,
    string Purpose,
    string FeeStatus,
    byte FeeStatusCode,
    decimal? FeeAmount,
    decimal? ProcedureChargeAmount,
    decimal? TotalChargeAmount,
    string VisitStatus,
    byte VisitStatusCode,
    DateOnly? ScheduledSurgeryDate,
    Guid PatientId,
    string PatientCode,
    string PatientFirstName,
    string PatientLastName,
    Guid ConsultingDoctorId,
    string DoctorFullName,
    string AggregatedPaymentStatus,
    byte AggregatedPaymentStatusCode);

public record VisitListResponse(
    IEnumerable<VisitListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record VisitSummaryResponse(
    int TotalVisits,
    int TotalCharged,
    int TotalFree,
    DateOnly? UpcomingScheduledSurgeryDate);
