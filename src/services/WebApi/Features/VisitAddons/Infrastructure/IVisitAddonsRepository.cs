namespace WebApi.Features.VisitAddons.Infrastructure;

public class VisitAddonRow
{
    public Guid VisitAddonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal DefaultAmount { get; set; }
    public byte AddonStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class VisitAddonLookupRow
{
    public Guid VisitAddonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal DefaultAmount { get; set; }
}

public interface IVisitAddonsRepository
{
    Task<(IEnumerable<VisitAddonRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct);

    Task<IEnumerable<VisitAddonLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct);

    Task<VisitAddonRow?> GetByIdAsync(Guid tenantId, Guid visitAddonId, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? visitAddonId,
        string name,
        string? code,
        decimal defaultAmount,
        Guid actorId,
        CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid visitAddonId, byte status, Guid actorId, CancellationToken ct);
}
