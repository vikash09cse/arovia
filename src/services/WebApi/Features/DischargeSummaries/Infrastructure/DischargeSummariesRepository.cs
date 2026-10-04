using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.DischargeSummaries.Infrastructure;

public class DischargeSummariesRepository(DbHelper dbHelper) : IDischargeSummariesRepository
{
    public async Task<DischargeSummaryGetRow?> GetAsync(Guid tenantId, Guid admissionId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        try
        {
            return await conn.QueryFirstOrDefaultAsync<DischargeSummaryGetRow>(
                "dbo.sp_discharge_summary_get",
                new { tenantid = tenantId, admissionid = admissionId },
                commandType: CommandType.StoredProcedure);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50401)
        {
            return null;
        }
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid admissionId,
        DateOnly? dateOfSurgery,
        DateOnly? dateOfDischarge,
        string? finalDiagnosis,
        string? diagnosisKey,
        int formSchemaVersion,
        string formJson,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_discharge_summary_save",
            new
            {
                tenantid = tenantId,
                admissionid = admissionId,
                dateofsurgery = dateOfSurgery?.ToDateTime(TimeOnly.MinValue),
                dateofdischarge = dateOfDischarge?.ToDateTime(TimeOnly.MinValue),
                finaldiagnosis = finalDiagnosis,
                diagnosiskey = diagnosisKey,
                formschemaversion = formSchemaVersion,
                formjson = formJson,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.dischargesummaryid;
    }

    public async Task<IReadOnlyList<DiagnosisMasterRow>> GetActiveDiagnosesAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<DiagnosisMasterRow>(
            "dbo.sp_diagnosis_master_get_active",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }
}
