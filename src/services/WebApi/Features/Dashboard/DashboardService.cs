using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Dashboard.Infrastructure;

namespace WebApi.Features.Dashboard;

public class DashboardService(
    IDashboardRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    public async Task<Result<TenantDashboardResponse>> GetDashboardAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<TenantDashboardResponse>();
        if (tenantError != null) return tenantError;

        var tenantId = httpContextAccessor.GetTenantContext().TenantId;
        var row = await repository.GetTenantDashboardAsync(tenantId, ct);

        return Result<TenantDashboardResponse>.Ok(new TenantDashboardResponse(
            row.TotalPatientCount,
            row.TodayNewPatientCount,
            row.TodayVisitCount,
            row.TodayRevenue,
            row.CurrentMonthRevenue,
            row.TotalPendingAmount,
            row.TodayLabAssignCount,
            row.CurrentMonthLabAssignCount));
    }
}
