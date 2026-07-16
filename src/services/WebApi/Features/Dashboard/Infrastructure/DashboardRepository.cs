using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Dashboard.Infrastructure;

public class DashboardRepository(DbHelper dbHelper) : IDashboardRepository
{
    public async Task<TenantDashboardRow> GetTenantDashboardAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var row = await conn.QueryFirstOrDefaultAsync<TenantDashboardRow>(
            "dbo.sp_get_tenant_dashboard",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);

        return row ?? new TenantDashboardRow();
    }
}
