using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Patients.Infrastructure;

public class PatientsRepository(DbHelper dbHelper) : IPatientsRepository
{
    public async Task<(IEnumerable<PatientRow> Items, int Total)> GetPatientsAsync(
        Guid tenantId, int page, int pageSize, string? patientCode, byte[]? phoneBlindIndex, byte? status, byte? gender, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<PatientRow>(
            "dbo.sp_get_patients",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                patientcode = patientCode,
                phoneblindindex = phoneBlindIndex,
                patientstatus = status,
                gender
            },
            commandType: CommandType.StoredProcedure);
        var list = rows.ToList();
        var total = list.FirstOrDefault()?.TotalCount ?? 0;
        return (list, total);
    }

    public async Task<PatientRow?> GetByIdAsync(Guid tenantId, Guid patientId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<PatientRow>(
            "dbo.sp_get_patient_by_id",
            new { tenantid = tenantId, patientid = patientId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<DuplicatePhoneRow?> PhoneExistsAsync(
        Guid tenantId, byte[] phoneBlindIndex, Guid? excludePatientId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<DuplicatePhoneRow>(
            "dbo.sp_patient_phone_exists",
            new { tenantid = tenantId, phoneblindindex = phoneBlindIndex, excludepatientid = excludePatientId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid? patientId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        int? age,
        byte gender,
        byte? bloodGroup,
        string? referredBy,
        EncryptedPatientPayload encrypted,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "dbo.sp_save_patient",
            new
            {
                patientid = patientId,
                tenantid = tenantId,
                firstname = firstName,
                lastname = lastName,
                dateofbirth = dateOfBirth?.ToDateTime(TimeOnly.MinValue),
                age,
                gender,
                bloodgroup = bloodGroup,
                referredby = referredBy,
                phonecipher = encrypted.PhoneCipher,
                emailcipher = encrypted.EmailCipher,
                addresscipher = encrypted.AddressCipher,
                emergencynamecipher = encrypted.EmergencyNameCipher,
                emergencyphonecipher = encrypted.EmergencyPhoneCipher,
                phoneblindindex = encrypted.PhoneBlindIndex,
                emailblindindex = encrypted.EmailBlindIndex,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetStatusAsync(Guid tenantId, Guid patientId, byte status, Guid updatedBy, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_set_patient_status",
            new { tenantid = tenantId, patientid = patientId, patientstatus = status, updatedby = updatedBy },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(Guid tenantId, Guid patientId, Guid updatedBy, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_delete_patient",
            new { tenantid = tenantId, patientid = patientId, updatedby = updatedBy },
            commandType: CommandType.StoredProcedure);
    }
}
