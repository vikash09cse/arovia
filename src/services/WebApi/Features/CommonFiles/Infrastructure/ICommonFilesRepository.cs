namespace WebApi.Features.CommonFiles.Infrastructure;

public class CommonFileRow
{
    public Guid CommonFileId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public interface ICommonFilesRepository
{
    Task<IEnumerable<CommonFileRow>> GetListAsync(Guid tenantId, CancellationToken ct);

    Task<CommonFileRow?> GetByIdAsync(Guid tenantId, Guid commonFileId, CancellationToken ct);

    Task<CommonFileRow> SaveAsync(
        Guid tenantId,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid commonFileId, Guid actorId, CancellationToken ct);
}
