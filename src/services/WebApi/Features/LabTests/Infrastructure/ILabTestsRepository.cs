namespace WebApi.Features.LabTests.Infrastructure;

public class LabAgencyRow
{
    public Guid LabAgencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public byte AgencyStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalCount { get; set; }
}

public class LabAgencyLookupRow
{
    public Guid LabAgencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
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

public class LabAgencyAssignmentReportRow
{
    public Guid LabAgencyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public byte AgencyStatus { get; set; }
    public int VisitCount { get; set; }
}

public interface ILabTestsRepository
{
    Task<(IEnumerable<LabAgencyRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct);

    Task<IEnumerable<LabAgencyLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct);

    Task<IEnumerable<LabAgencyAssignmentReportRow>> GetAssignmentReportAsync(
        Guid tenantId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? patientCode,
        byte[]? phoneBlindIndex,
        CancellationToken ct);

    Task<LabAgencyRow?> GetByIdAsync(Guid tenantId, Guid labAgencyId, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? labAgencyId,
        string name,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        string? notes,
        Guid actorId,
        CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid labAgencyId, byte status, Guid actorId, CancellationToken ct);

    Task<VisitLabAgencyRow?> AssignToVisitAsync(
        Guid tenantId, Guid visitId, Guid labAgencyId, string? testName, string? notes, Guid actorId, CancellationToken ct);

    Task RemoveFromVisitAsync(
        Guid tenantId, Guid visitId, Guid visitLabAgencyId, Guid actorId, CancellationToken ct);
}
