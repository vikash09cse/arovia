namespace WebApi.Features.UserSalaries.Infrastructure;

public class UserSalaryRow
{
    public Guid UserSalaryId { get; set; }
    public Guid UserId { get; set; }
    public decimal MonthlySalary { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

public interface IUserSalariesRepository
{
    Task<IEnumerable<UserSalaryRow>> GetListAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<UserSalaryRow?> GetCurrentAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<UserSalaryRow> AddAsync(
        Guid tenantId,
        Guid userId,
        decimal monthlySalary,
        DateOnly effectiveFrom,
        string? notes,
        Guid actorId,
        CancellationToken ct);
}
