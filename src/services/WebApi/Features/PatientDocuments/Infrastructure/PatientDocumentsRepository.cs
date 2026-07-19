using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.PatientDocuments.Infrastructure;

public class PatientDocumentsRepository(DbHelper dbHelper) : IPatientDocumentsRepository
{
    public async Task<IEnumerable<PatientDocumentRow>> GetListAsync(
        Guid tenantId, Guid patientId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<PatientDocumentRow>(
            "dbo.sp_patient_document_get_list",
            new { tenantid = tenantId, patientid = patientId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<PatientDocumentRow?> GetByIdAsync(
        Guid tenantId, Guid patientId, Guid patientDocumentId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<PatientDocumentRow>(
            "dbo.sp_patient_document_get_by_id",
            new { tenantid = tenantId, patientid = patientId, patientdocumentid = patientDocumentId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<PatientDocumentRow> SaveAsync(
        Guid tenantId,
        Guid patientId,
        string displayName,
        string storedFileName,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstAsync<PatientDocumentRow>(
            "dbo.sp_patient_document_save",
            new
            {
                tenantid = tenantId,
                patientid = patientId,
                displayname = displayName,
                storedfilename = storedFileName,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(
        Guid tenantId, Guid patientId, Guid patientDocumentId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_patient_document_delete",
            new
            {
                tenantid = tenantId,
                patientid = patientId,
                patientdocumentid = patientDocumentId,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
