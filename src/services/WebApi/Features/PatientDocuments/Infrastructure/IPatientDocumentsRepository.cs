namespace WebApi.Features.PatientDocuments.Infrastructure;

public class PatientDocumentRow
{
    public Guid PatientDocumentId { get; set; }
    public Guid PatientId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public interface IPatientDocumentsRepository
{
    Task<IEnumerable<PatientDocumentRow>> GetListAsync(Guid tenantId, Guid patientId, CancellationToken ct);

    Task<PatientDocumentRow?> GetByIdAsync(
        Guid tenantId, Guid patientId, Guid patientDocumentId, CancellationToken ct);

    Task<PatientDocumentRow> SaveAsync(
        Guid tenantId,
        Guid patientId,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct);

    Task DeleteAsync(
        Guid tenantId, Guid patientId, Guid patientDocumentId, Guid actorId, CancellationToken ct);
}
