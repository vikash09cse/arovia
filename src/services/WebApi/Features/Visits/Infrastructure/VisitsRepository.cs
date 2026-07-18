using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;
using System.Text.Json;

namespace WebApi.Features.Visits.Infrastructure;

public class VisitsRepository(DbHelper dbHelper) : IVisitsRepository
{
    public async Task<FeePreviewRow?> GetFeePreviewAsync(Guid tenantId, Guid patientId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<FeePreviewRow>(
            "dbo.sp_visit_get_fee_preview",
            new { tenantid = tenantId, patientid = patientId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid patientId,
        Guid consultingDoctorId,
        byte visitType,
        string? purpose,
        string? visitNotes,
        decimal? procedureChargeAmount,
        DateOnly? scheduledSurgeryDate,
        byte feeStatus,
        decimal? consultationFeeAmount,
        string? feeNote,
        decimal? initialCollectionAmount,
        Guid? collectedByUserId,
        IReadOnlyList<Guid>? addonIds,
        decimal? discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var addonIdsJson = addonIds is { Count: > 0 }
            ? JsonSerializer.Serialize(addonIds)
            : null;
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_visit_save",
            new
            {
                tenantid = tenantId,
                patientid = patientId,
                consultingdoctorid = consultingDoctorId,
                visittype = visitType,
                purpose,
                visitnotes = visitNotes,
                procedurechargeamount = procedureChargeAmount,
                scheduledsurgerydate = scheduledSurgeryDate?.ToDateTime(TimeOnly.MinValue),
                overridefeestatus = feeStatus,
                consultationfeeamount = consultationFeeAmount,
                feeoverridereason = feeNote,
                initialcollectionamount = initialCollectionAmount,
                collectedby = collectedByUserId,
                addonids = addonIdsJson,
                discountamount = discountAmount,
                discountreason = discountReason,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.visitid;
    }

    public async Task<(IEnumerable<VisitListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        Guid? patientId,
        string? patientCode,
        string? visitCode,
        byte[]? phoneBlindIndex,
        Guid? consultingDoctorId,
        byte? visitType,
        byte? feeStatus,
        byte? visitStatus,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<VisitListRow>(
            "dbo.sp_visit_get_list",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                patientid = patientId,
                patientcode = patientCode,
                visitcode = visitCode,
                phoneblindindex = phoneBlindIndex,
                consultingdoctorid = consultingDoctorId,
                visittype = visitType,
                feestatus = feeStatus,
                visitstatus = visitStatus,
                datefrom = dateFrom?.ToDateTime(TimeOnly.MinValue),
                dateto = dateTo?.ToDateTime(TimeOnly.MinValue)
            },
            commandType: CommandType.StoredProcedure);
        var list = rows.ToList();
        var total = list.FirstOrDefault()?.TotalCount ?? 0;
        return (list, total);
    }

    public async Task<(VisitDetailRow? Visit, IEnumerable<PaymentLineRow> PaymentLines, IEnumerable<VisitLabAgencyRow> LabAgencies, IEnumerable<VisitAddonLineRow> Addons)> GetByIdAsync(
        Guid tenantId, Guid visitId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_visit_get_by_id",
            new { tenantid = tenantId, visitid = visitId },
            commandType: CommandType.StoredProcedure);
        var visit = await multi.ReadFirstOrDefaultAsync<VisitDetailRow>();
        var payments = (await multi.ReadAsync<PaymentLineRow>()).ToList();
        var labAgencies = (await multi.ReadAsync<VisitLabAgencyRow>()).ToList();
        var addons = (await multi.ReadAsync<VisitAddonLineRow>()).ToList();
        return (visit, payments, labAgencies, addons);
    }

    public async Task<VisitSummaryRow?> GetPatientSummaryAsync(Guid tenantId, Guid patientId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<VisitSummaryRow>(
            "dbo.sp_visit_get_patient_summary",
            new { tenantid = tenantId, patientid = patientId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateNotesAsync(Guid tenantId, Guid visitId, string visitNotes, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_update_notes",
            new { tenantid = tenantId, visitid = visitId, visitnotes = visitNotes, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task OverrideFeeAsync(Guid tenantId, Guid visitId, byte feeStatus, string reason, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_override_fee",
            new { tenantid = tenantId, visitid = visitId, feestatus = feeStatus, feeoverridereason = reason, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task ApplyDiscountAsync(
        Guid tenantId,
        Guid visitId,
        decimal discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_apply_discount",
            new
            {
                tenantid = tenantId,
                visitid = visitId,
                discountamount = discountAmount,
                discountreason = discountReason,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(Guid tenantId, Guid visitId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_visit_delete",
            new { tenantid = tenantId, visitid = visitId, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<DoctorRow>> GetActiveDoctorsAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<DoctorRow>(
            "dbo.sp_visit_get_active_doctors",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<DoctorRow>> GetPaymentCollectorsAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryAsync<DoctorRow>(
            "dbo.sp_get_payment_collectors",
            new { tenantid = tenantId },
            commandType: CommandType.StoredProcedure);
    }
}
