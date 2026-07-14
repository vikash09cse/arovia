using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.LabTests.Infrastructure;

public class LabTestsRepository(DbHelper dbHelper) : ILabTestsRepository
{
    public async Task<(IEnumerable<LabAgencyRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = (await conn.QueryAsync<LabAgencyRow>(
            "dbo.sp_lab_agency_get_list",
            new { tenantid = tenantId, page, pagesize = pageSize, filter, agencystatus = status },
            commandType: CommandType.StoredProcedure)).ToList();
        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return (rows, total);
    }

    public async Task<IEnumerable<LabAgencyLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<LabAgencyLookupRow>(
            "dbo.sp_lab_agency_get_active",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<LabAgencyAssignmentReportRow>> GetAssignmentReportAsync(
        Guid tenantId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? patientCode,
        byte[]? phoneBlindIndex,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<LabAgencyAssignmentReportRow>(
            "dbo.sp_lab_agency_assignment_report",
            new
            {
                tenantid = tenantId,
                datefrom = dateFrom?.ToDateTime(TimeOnly.MinValue),
                dateto = dateTo?.ToDateTime(TimeOnly.MinValue),
                patientcode = patientCode,
                phoneblindindex = phoneBlindIndex
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<LabAgencyRow?> GetByIdAsync(Guid tenantId, Guid labAgencyId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<LabAgencyRow>(
            "dbo.sp_lab_agency_get_by_id",
            new { tenantid = tenantId, labagencyid = labAgencyId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? labAgencyId,
        string name,
        string? contactPerson,
        string? phone,
        string? email,
        string? address,
        string? notes,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_lab_agency_save",
            new
            {
                tenantid = tenantId,
                labagencyid = labAgencyId,
                name,
                contactperson = contactPerson,
                phone,
                email,
                address,
                notes,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.labagencyid;
    }

    public async Task SetStatusAsync(Guid tenantId, Guid labAgencyId, byte status, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_lab_agency_set_status",
            new { tenantid = tenantId, labagencyid = labAgencyId, agencystatus = status, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<VisitLabAgencyRow?> AssignToVisitAsync(
        Guid tenantId, Guid visitId, Guid labAgencyId, string? testName, string? notes, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<VisitLabAgencyRow>(
            "dbo.sp_visit_lab_agency_assign",
            new
            {
                tenantid = tenantId,
                visitid = visitId,
                labagencyid = labAgencyId,
                testname = testName,
                notes,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task RemoveFromVisitAsync(
        Guid tenantId, Guid visitId, Guid visitLabAgencyId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_lab_agency_remove",
            new
            {
                tenantid = tenantId,
                visitid = visitId,
                visitlabagencyid = visitLabAgencyId,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
