using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Admissions.Infrastructure;

public class AdmissionsRepository(DbHelper dbHelper) : IAdmissionsRepository
{
    public async Task<(IEnumerable<AdmissionListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        byte? admissionStatus,
        Guid? patientId,
        string? admissionCode,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = await conn.QueryAsync<AdmissionListRow>(
            "dbo.sp_admission_get_list",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                admissionstatus = admissionStatus,
                patientid = patientId,
                admissioncode = admissionCode,
                datefrom = dateFrom?.ToDateTime(TimeOnly.MinValue),
                dateto = dateTo?.ToDateTime(TimeOnly.MinValue)
            },
            commandType: CommandType.StoredProcedure);
        var list = rows.ToList();
        return (list, list.FirstOrDefault()?.TotalCount ?? 0);
    }

    public async Task<(AdmissionDetailRow? Admission, IEnumerable<AdmissionChargeRow> Charges, IEnumerable<AdmissionPaymentRow> Payments)> GetByIdAsync(
        Guid tenantId,
        Guid admissionId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_admission_get_by_id",
            new { tenantid = tenantId, admissionid = admissionId },
            commandType: CommandType.StoredProcedure);
        var admission = await multi.ReadFirstOrDefaultAsync<AdmissionDetailRow>();
        var charges = (await multi.ReadAsync<AdmissionChargeRow>()).ToList();
        var payments = (await multi.ReadAsync<AdmissionPaymentRow>()).ToList();
        return (admission, charges, payments);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        Guid patientId,
        Guid departmentId,
        Guid attendingDoctorId,
        string ward,
        string? bed,
        string roomClass,
        decimal estimatedAmount,
        string? notes,
        Guid? fromVisitId,
        decimal? depositAmount,
        byte? depositPaymentMethod,
        Guid? collectedBy,
        DateOnly? admissionDate,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_admission_save",
            new
            {
                tenantid = tenantId,
                patientid = patientId,
                departmentid = departmentId,
                attendingdoctorid = attendingDoctorId,
                ward,
                bed,
                roomclass = roomClass,
                estimatedamount = estimatedAmount,
                notes,
                fromvisitid = fromVisitId,
                depositamount = depositAmount,
                depositpaymentmethod = depositPaymentMethod,
                collectedby = collectedBy,
                admissiondate = admissionDate,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.admissionid;
    }

    public async Task<Guid> AddChargeAsync(
        Guid tenantId,
        Guid admissionId,
        byte chargeCategory,
        string description,
        decimal amount,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_admission_add_charge",
            new
            {
                tenantid = tenantId,
                admissionid = admissionId,
                chargecategory = chargeCategory,
                description,
                amount,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.admissionchargeid;
    }

    public async Task ApplyDiscountAsync(
        Guid tenantId,
        Guid admissionId,
        decimal discountAmount,
        string? discountReason,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_admission_apply_discount",
            new
            {
                tenantid = tenantId,
                admissionid = admissionId,
                discountamount = discountAmount,
                discountreason = discountReason,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Guid> AddPaymentAsync(
        Guid tenantId,
        Guid admissionId,
        decimal amount,
        byte paymentMethod,
        byte paymentKind,
        string? notes,
        Guid collectedBy,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_admission_add_payment",
            new
            {
                tenantid = tenantId,
                admissionid = admissionId,
                amount,
                paymentmethod = paymentMethod,
                paymentkind = paymentKind,
                notes,
                collectedby = collectedBy,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.admissionpaymentid;
    }

    public async Task DischargeAsync(Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_admission_discharge",
            new { tenantid = tenantId, admissionid = admissionId, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_admission_delete",
            new { tenantid = tenantId, admissionid = admissionId, actorid = actorId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<string> EnsureInvoiceNumberAsync(
        Guid tenantId, Guid admissionId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var p = new DynamicParameters();
        p.Add("tenantid", tenantId);
        p.Add("admissionid", admissionId);
        p.Add("actorid", actorId);
        p.Add("invoicenumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        await conn.ExecuteAsync(
            "dbo.sp_admission_ensure_invoice_number",
            p,
            commandType: CommandType.StoredProcedure);
        return p.Get<string>("invoicenumber") ?? string.Empty;
    }

    public async Task<(AdmissionFinalInvoiceHeaderRow? Header, IReadOnlyList<AdmissionFinalInvoiceChargeRow> Charges, IReadOnlyList<AdmissionFinalInvoicePaymentRow> Payments)> GetFinalInvoiceDataAsync(
        Guid tenantId,
        Guid admissionId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_admission_get_final_invoice",
            new { tenantid = tenantId, admissionid = admissionId },
            commandType: CommandType.StoredProcedure);

        var header = await multi.ReadFirstOrDefaultAsync<AdmissionFinalInvoiceHeaderRow>();
        var charges = (await multi.ReadAsync<AdmissionFinalInvoiceChargeRow>()).ToList();
        var payments = (await multi.ReadAsync<AdmissionFinalInvoicePaymentRow>()).ToList();
        return (header, charges, payments);
    }
}
