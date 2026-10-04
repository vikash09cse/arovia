using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.MonthlyReconcile.Infrastructure;

public class MonthlyReconcileRepository(DbHelper dbHelper) : IMonthlyReconcileRepository
{
    public async Task<(MonthlyProfileRow? Profile, MonthlyPreviewRow Preview)> GetAsync(
        Guid tenantId, string yearMonth, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_monthly_profile_get",
            new { tenantid = tenantId, yearmonth = yearMonth },
            commandType: CommandType.StoredProcedure);

        var profile = await multi.ReadFirstOrDefaultAsync<MonthlyProfileRow>();
        var preview = await multi.ReadSingleAsync<MonthlyPreviewRow>();
        return (profile, preview);
    }

    public async Task<IReadOnlyList<MonthlyProfileRow>> GetHistoryAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<MonthlyProfileRow>(
            "dbo.sp_monthly_profile_get_history",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<MonthlyProfileRow> ReconcileAsync(
        Guid tenantId, string yearMonth, string? note, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QuerySingleAsync<MonthlyProfileRow>(
            "dbo.sp_monthly_profile_reconcile",
            new
            {
                tenantid = tenantId,
                yearmonth = yearMonth,
                note,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task ReopenAsync(Guid tenantId, string yearMonth, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_monthly_profile_reopen",
            new
            {
                tenantid = tenantId,
                yearmonth = yearMonth,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
