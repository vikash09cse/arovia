namespace WebApi.Features.Dashboard.Infrastructure;

public class TenantDashboardRow
{
    public int TotalPatientCount { get; set; }
    public int TodayNewPatientCount { get; set; }
    public int TodayVisitCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal CurrentMonthRevenue { get; set; }
    public decimal TotalPendingAmount { get; set; }
    public int TodayLabAssignCount { get; set; }
    public int CurrentMonthLabAssignCount { get; set; }
}

public interface IDashboardRepository
{
    Task<TenantDashboardRow> GetTenantDashboardAsync(Guid tenantId, CancellationToken ct);
}
