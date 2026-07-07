namespace WebApi.Features.Doctors.Infrastructure;

public class DoctorRow
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public byte Role { get; set; }
    public byte Status { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalCount { get; set; }
}

public interface IDoctorsRepository
{
    Task<(IEnumerable<DoctorRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct);

    Task<DoctorRow?> GetByIdAsync(Guid tenantId, Guid doctorId, CancellationToken ct);

    Task<bool> EmailExistsAsync(Guid tenantId, string email, Guid? excludeId, CancellationToken ct);

    Task<Guid> CreateAsync(
        Guid tenantId, string email, string firstName, string lastName, string passwordHash,
        Guid createdBy, CancellationToken ct);

    Task UpdateAsync(Guid tenantId, Guid doctorId, string firstName, string lastName, Guid updatedBy, CancellationToken ct);

    Task SetStatusAsync(Guid tenantId, Guid doctorId, byte status, Guid updatedBy, CancellationToken ct);

    Task<IEnumerable<DoctorRow>> GetActiveAsync(Guid tenantId, CancellationToken ct);
}
