using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Departments.Infrastructure;

public class DepartmentsRepository(DbHelper dbHelper) : IDepartmentsRepository
{
    public async Task<(IEnumerable<DepartmentRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<DepartmentRow>(
            "dbo.sp_department_get_list",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                filter,
                departmentstatus = status
            },
            commandType: CommandType.StoredProcedure);
        var list = rows.ToList();
        return (list, list.FirstOrDefault()?.TotalCount ?? 0);
    }

    public async Task<IEnumerable<DepartmentLookupRow>> GetActiveAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<DepartmentLookupRow>(
            "dbo.sp_department_get_active",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<DepartmentRow?> GetByIdAsync(Guid tenantId, Guid departmentId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<DepartmentRow>(
            "dbo.sp_department_get_by_id",
            new { tenantid = tenantId, departmentid = departmentId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(Guid tenantId, Guid? departmentId, string name, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_department_save",
            new
            {
                tenantid = tenantId,
                departmentid = departmentId,
                name,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.departmentid;
    }

    public async Task SetStatusAsync(Guid tenantId, Guid departmentId, byte status, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_department_set_status",
            new
            {
                tenantid = tenantId,
                departmentid = departmentId,
                departmentstatus = status,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
