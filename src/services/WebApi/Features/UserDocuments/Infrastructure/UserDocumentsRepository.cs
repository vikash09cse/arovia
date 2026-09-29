using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.UserDocuments.Infrastructure;

public class UserDocumentsRepository(DbHelper dbHelper) : IUserDocumentsRepository
{
    public async Task<IEnumerable<UserDocumentRow>> GetListAsync(
        Guid tenantId, Guid userId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<UserDocumentRow>(
            "dbo.sp_user_document_get_list",
            new { tenantid = tenantId, userid = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserDocumentRow?> GetByIdAsync(
        Guid tenantId, Guid userId, Guid userDocumentId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<UserDocumentRow>(
            "dbo.sp_user_document_get_by_id",
            new { tenantid = tenantId, userid = userId, userdocumentid = userDocumentId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserDocumentRow> SaveAsync(
        Guid tenantId,
        Guid userId,
        byte documentType,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstAsync<UserDocumentRow>(
            "dbo.sp_user_document_save",
            new
            {
                tenantid = tenantId,
                userid = userId,
                documenttype = documentType,
                displayname = displayName,
                storedfilename = storedFileName,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(
        Guid tenantId, Guid userId, Guid userDocumentId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_user_document_delete",
            new
            {
                tenantid = tenantId,
                userid = userId,
                userdocumentid = userDocumentId,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
