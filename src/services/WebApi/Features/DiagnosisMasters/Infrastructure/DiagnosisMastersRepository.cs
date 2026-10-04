using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.DiagnosisMasters.Infrastructure;

public class DiagnosisMastersRepository(DbHelper dbHelper) : IDiagnosisMastersRepository
{
    public async Task<IReadOnlyList<DiagnosisMasterAdminRow>> GetActiveAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<DiagnosisMasterAdminRow>(
            "dbo.sp_diagnosis_master_get_active",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<(IReadOnlyList<DiagnosisMasterAdminRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, bool? isActive, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = (await conn.QueryAsync<DiagnosisMasterAdminRow>(
            "dbo.sp_diagnosis_master_get_list",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                filter,
                isactive = isActive
            },
            commandType: CommandType.StoredProcedure)).ToList();
        return (rows, rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    public async Task<DiagnosisMasterAdminRow?> GetByIdAsync(
        Guid tenantId, Guid diagnosisMasterId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<DiagnosisMasterAdminRow>(
            "dbo.sp_diagnosis_master_get_by_id",
            new { tenantid = tenantId, diagnosismasterid = diagnosisMasterId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? diagnosisMasterId,
        string? code,
        string name,
        string? packJson,
        int sortOrder,
        bool isActive,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_diagnosis_master_save",
            new
            {
                tenantid = tenantId,
                diagnosismasterid = diagnosisMasterId,
                code,
                name,
                packjson = packJson,
                sortorder = sortOrder,
                isactive = isActive
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.diagnosismasterid;
    }

    public async Task SetStatusAsync(
        Guid tenantId, Guid diagnosisMasterId, bool isActive, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_diagnosis_master_set_status",
            new
            {
                tenantid = tenantId,
                diagnosismasterid = diagnosisMasterId,
                isactive = isActive
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(Guid tenantId, Guid diagnosisMasterId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_diagnosis_master_delete",
            new { tenantid = tenantId, diagnosismasterid = diagnosisMasterId },
            commandType: CommandType.StoredProcedure);
    }
}
