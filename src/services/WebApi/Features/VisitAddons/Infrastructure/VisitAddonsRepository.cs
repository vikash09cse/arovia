using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.VisitAddons.Infrastructure;

public class VisitAddonsRepository(DbHelper dbHelper) : IVisitAddonsRepository
{
    public async Task<(IEnumerable<VisitAddonRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = (await conn.QueryAsync<VisitAddonRow>(
            "dbo.sp_visit_addon_catalog_get_list",
            new { tenantid = tenantId, page, pagesize = pageSize, filter, addonstatus = status },
            commandType: CommandType.StoredProcedure)).ToList();
        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return (rows, total);
    }

    public async Task<IEnumerable<VisitAddonLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<VisitAddonLookupRow>(
            "dbo.sp_visit_addon_catalog_get_active",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<VisitAddonRow?> GetByIdAsync(Guid tenantId, Guid visitAddonId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<VisitAddonRow>(
            "dbo.sp_visit_addon_catalog_get_by_id",
            new { tenantid = tenantId, visitaddonid = visitAddonId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? visitAddonId,
        string name,
        string? code,
        decimal defaultAmount,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_visit_addon_catalog_save",
            new
            {
                tenantid = tenantId,
                visitaddonid = visitAddonId,
                name,
                code,
                defaultamount = defaultAmount,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.visitaddonid;
    }

    public async Task SetStatusAsync(Guid tenantId, Guid visitAddonId, byte status, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_addon_catalog_set_status",
            new { tenantid = tenantId, visitaddonid = visitAddonId, addonstatus = status, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }
}
