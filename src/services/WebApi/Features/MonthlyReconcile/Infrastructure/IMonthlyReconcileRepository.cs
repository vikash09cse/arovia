namespace WebApi.Features.MonthlyReconcile.Infrastructure;

public class MonthlyProfileRow
{
    public Guid MonthlyProfileId { get; set; }
    public string YearMonth { get; set; } = string.Empty;
    public byte ProfileStatus { get; set; }
    public decimal OpdRevenue { get; set; }
    public decimal IpdRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Profit { get; set; }
    public string? Note { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public Guid? ReconciledBy { get; set; }
    public string? ReconcilerName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MonthlyPreviewRow
{
    public decimal OpdRevenue { get; set; }
    public decimal IpdRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Profit { get; set; }
}

public interface IMonthlyReconcileRepository
{
    Task<(MonthlyProfileRow? Profile, MonthlyPreviewRow Preview)> GetAsync(
        Guid tenantId, string yearMonth, CancellationToken ct);

    Task<IReadOnlyList<MonthlyProfileRow>> GetHistoryAsync(Guid tenantId, CancellationToken ct);

    Task<MonthlyProfileRow> ReconcileAsync(
        Guid tenantId, string yearMonth, string? note, Guid actorId, CancellationToken ct);

    Task ReopenAsync(Guid tenantId, string yearMonth, Guid actorId, CancellationToken ct);
}
