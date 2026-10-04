namespace WebApi.Features.Departments.Infrastructure;

public class DepartmentRow
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte DepartmentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class DepartmentLookupRow
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IDepartmentsRepository
{
    Task<(IEnumerable<DepartmentRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct);

    Task<IEnumerable<DepartmentLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct);

    Task<DepartmentRow?> GetByIdAsync(Guid tenantId, Guid departmentId, CancellationToken ct);

    Task<Guid> SaveAsync(Guid tenantId, Guid? departmentId, string name, Guid actorId, CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid departmentId, byte status, Guid actorId, CancellationToken ct);
}
