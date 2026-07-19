using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.TenantSettings.Infrastructure;

public class TenantSettingsRepository(DbHelper dbHelper) : ITenantSettingsRepository
{
    public async Task<TenantSettingsRow?> GetAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<TenantSettingsRow>(
            "dbo.sp_get_tenant_by_id",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateAsync(
        Guid tenantId,
        string hospitalName,
        string primaryContactFirstName,
        string primaryContactLastName,
        string primaryContactEmail,
        string primaryContactPhone,
        string address,
        string timezone,
        string? website,
        string? logoUrl,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_update_tenant",
            new
            {
                tenantid = tenantId,
                hospitalname = hospitalName,
                primarycontactfirstname = primaryContactFirstName,
                primarycontactlastname = primaryContactLastName,
                primarycontactemail = primaryContactEmail,
                primarycontactphone = primaryContactPhone,
                tenantaddress = address,
                timezone,
                logourl = logoUrl,
                website
            },
            commandType: CommandType.StoredProcedure);
    }
}
