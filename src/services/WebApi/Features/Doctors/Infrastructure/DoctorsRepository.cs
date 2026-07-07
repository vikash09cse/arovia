using Dapper;
using SharedKernel.Enums;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Doctors.Infrastructure;

public class DoctorsRepository(DbHelper dbHelper) : IDoctorsRepository
{
    public async Task<(IEnumerable<DoctorRow> Items, int Total)> GetListAsync(
        Guid tenantId, int page, int pageSize, string? filter, byte? status, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<DoctorRow>(
            "dbo.sp_doctor_get_list",
            new { tenantid = tenantId, page, pagesize = pageSize, filter, userstatus = status },
            commandType: CommandType.StoredProcedure);
        var list = rows.ToList();
        return (list, list.FirstOrDefault()?.TotalCount ?? 0);
    }

    public async Task<DoctorRow?> GetByIdAsync(Guid tenantId, Guid doctorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<DoctorRow>(
            "dbo.sp_doctor_get_by_id",
            new { tenantid = tenantId, userid = doctorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> EmailExistsAsync(Guid tenantId, string email, Guid? excludeId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "dbo.sp_tenant_user_email_exists",
            new { tenantid = tenantId, email, excludeid = excludeId },
            commandType: CommandType.StoredProcedure);
        return count > 0;
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId, string email, string firstName, string lastName, string passwordHash,
        Guid createdBy, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "dbo.sp_create_tenant_user",
            new
            {
                userid = Guid.NewGuid(),
                tenantid = tenantId,
                email,
                passwordhash = passwordHash,
                firstname = firstName,
                lastname = lastName,
                usertype = (byte)UserType.Doctor,
                userstatus = (byte)UserStatus.Active,
                createdby = createdBy
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateAsync(Guid tenantId, Guid doctorId, string firstName, string lastName, Guid updatedBy, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_update_tenant_user",
            new
            {
                tenantid = tenantId,
                userid = doctorId,
                firstname = firstName,
                lastname = lastName,
                usertype = (byte)UserType.Doctor,
                updatedby = updatedBy
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetStatusAsync(Guid tenantId, Guid doctorId, byte status, Guid updatedBy, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_set_tenant_user_status",
            new { tenantid = tenantId, userid = doctorId, userstatus = status, updatedby = updatedBy },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<DoctorRow>> GetActiveAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<DoctorRow>(
            "dbo.sp_visit_get_active_doctors",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }
}
