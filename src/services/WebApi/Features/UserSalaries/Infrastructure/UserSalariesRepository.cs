using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.UserSalaries.Infrastructure;

public class UserSalariesRepository(DbHelper dbHelper) : IUserSalariesRepository
{
    public async Task<IEnumerable<UserSalaryRow>> GetListAsync(
        Guid tenantId, Guid userId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<UserSalaryRow>(
            "dbo.sp_user_salary_get_list",
            new { tenantid = tenantId, userid = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserSalaryRow?> GetCurrentAsync(
        Guid tenantId, Guid userId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<UserSalaryRow>(
            "dbo.sp_user_salary_get_current",
            new { tenantid = tenantId, userid = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserSalaryRow> AddAsync(
        Guid tenantId,
        Guid userId,
        decimal monthlySalary,
        DateOnly effectiveFrom,
        string? notes,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstAsync<UserSalaryRow>(
            "dbo.sp_user_salary_add",
            new
            {
                tenantid = tenantId,
                userid = userId,
                monthlysalary = monthlySalary,
                effectivefrom = effectiveFrom.ToDateTime(TimeOnly.MinValue),
                notes,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
