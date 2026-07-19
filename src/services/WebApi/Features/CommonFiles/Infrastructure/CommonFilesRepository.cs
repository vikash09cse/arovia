using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.CommonFiles.Infrastructure;

public class CommonFilesRepository(DbHelper dbHelper) : ICommonFilesRepository
{
    public async Task<IEnumerable<CommonFileRow>> GetListAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<CommonFileRow>(
            "dbo.sp_common_file_get_list",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<CommonFileRow?> GetByIdAsync(Guid tenantId, Guid commonFileId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<CommonFileRow>(
            "dbo.sp_common_file_get_by_id",
            new { tenantid = tenantId, commonfileid = commonFileId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<CommonFileRow> SaveAsync(
        Guid tenantId,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstAsync<CommonFileRow>(
            "dbo.sp_common_file_save",
            new
            {
                tenantid = tenantId,
                displayname = displayName,
                storedfilename = storedFileName,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(Guid tenantId, Guid commonFileId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_common_file_delete",
            new { tenantid = tenantId, commonfileid = commonFileId, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }
}
