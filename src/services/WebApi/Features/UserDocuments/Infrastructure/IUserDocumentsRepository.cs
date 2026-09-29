namespace WebApi.Features.UserDocuments.Infrastructure;

public class UserDocumentRow
{
    public Guid UserDocumentId { get; set; }
    public Guid UserId { get; set; }
    public byte DocumentType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public interface IUserDocumentsRepository
{
    Task<IEnumerable<UserDocumentRow>> GetListAsync(Guid tenantId, Guid userId, CancellationToken ct);

    Task<UserDocumentRow?> GetByIdAsync(
        Guid tenantId, Guid userId, Guid userDocumentId, CancellationToken ct);

    Task<UserDocumentRow> SaveAsync(
        Guid tenantId,
        Guid userId,
        byte documentType,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct);

    Task DeleteAsync(
        Guid tenantId, Guid userId, Guid userDocumentId, Guid actorId, CancellationToken ct);
}
