using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Payments.Infrastructure;

public class PaymentsRepository(DbHelper dbHelper) : IPaymentsRepository
{
    public async Task<(IEnumerable<PaymentListRow> Items, int Total)> GetListAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? patientCode,
        bool openVisitsOnly,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var rows = (await conn.QueryAsync<PaymentListRow>(
            "dbo.sp_payment_get_list",
            new
            {
                tenantid = tenantId,
                page,
                pagesize = pageSize,
                patientcode = patientCode,
                openvisitsonly = openVisitsOnly,
                datefrom = dateFrom?.ToDateTime(TimeOnly.MinValue),
                dateto = dateTo?.ToDateTime(TimeOnly.MinValue)
            },
            commandType: CommandType.StoredProcedure)).ToList();

        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return (rows, total);
    }

    public async Task<AddCollectionResultRow?> AddCollectionAsync(
        Guid tenantId,
        Guid visitId,
        decimal amount,
        Guid collectedByUserId,
        string? notes,
        byte? paymentMethod,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<AddCollectionResultRow>(
            "dbo.sp_payment_add_collection",
            new
            {
                tenantid = tenantId,
                visitid = visitId,
                amount,
                collectedby = collectedByUserId,
                notes,
                paymentmethod = paymentMethod,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task VoidCollectionAsync(
        Guid tenantId,
        Guid paymentId,
        string? reason,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_payment_void_collection",
            new
            {
                tenantid = tenantId,
                paymentid = paymentId,
                reason,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CollectVisitPendingAsync(
        Guid tenantId,
        Guid visitId,
        Guid? collectedByUserId,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        return await conn.ExecuteAsync(
            "dbo.sp_payment_collect_visit_pending",
            new
            {
                tenantid = tenantId,
                visitid = visitId,
                actorid = actorId,
                collectedby = collectedByUserId
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(PaymentReceiptRow? Receipt, IReadOnlyList<PaymentReceiptAddonRow> Addons)> GetReceiptAsync(
        Guid tenantId,
        Guid paymentId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_payment_get_receipt",
            new { tenantid = tenantId, paymentid = paymentId },
            commandType: CommandType.StoredProcedure);

        var receipt = await multi.ReadFirstOrDefaultAsync<PaymentReceiptRow>();
        var addons = (await multi.ReadAsync<PaymentReceiptAddonRow>()).ToList();
        return (receipt, addons);
    }
}
