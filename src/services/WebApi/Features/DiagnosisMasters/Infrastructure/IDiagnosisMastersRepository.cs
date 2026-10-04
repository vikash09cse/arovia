namespace WebApi.Features.DiagnosisMasters.Infrastructure;

public class DiagnosisMasterAdminRow
{
    public Guid DiagnosisMasterId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PackJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public interface IDiagnosisMastersRepository
{
    Task<IReadOnlyList<DiagnosisMasterAdminRow>> GetActiveAsync(Guid tenantId, CancellationToken ct);

    Task<(IReadOnlyList<DiagnosisMasterAdminRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, bool? isActive, CancellationToken ct);

    Task<DiagnosisMasterAdminRow?> GetByIdAsync(Guid tenantId, Guid diagnosisMasterId, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? diagnosisMasterId,
        string? code,
        string name,
        string? packJson,
        int sortOrder,
        bool isActive,
        CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid diagnosisMasterId, bool isActive, CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid diagnosisMasterId, CancellationToken ct);
}
