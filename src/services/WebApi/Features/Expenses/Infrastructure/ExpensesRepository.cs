using Dapper;
using SharedKernel.Utilities.Helpers;
using System.Data;

namespace WebApi.Features.Expenses.Infrastructure;

public class ExpensesRepository(DbHelper dbHelper) : IExpensesRepository
{
    public async Task<(ExpenseMonthSummaryRow Summary, IReadOnlyList<ExpenseCategoryTotalRow> Categories, IReadOnlyList<ExpenseRow> Items)>
        GetByMonthAsync(Guid tenantId, string yearMonth, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        using var multi = await conn.QueryMultipleAsync(
            "dbo.sp_expense_get_by_month",
            new { tenantid = tenantId, yearmonth = yearMonth },
            commandType: CommandType.StoredProcedure);

        var summary = await multi.ReadSingleAsync<ExpenseMonthSummaryRow>();
        var categories = (await multi.ReadAsync<ExpenseCategoryTotalRow>()).ToList();
        var items = (await multi.ReadAsync<ExpenseRow>()).ToList();
        return (summary, categories, items);
    }

    public async Task<Guid> SaveAsync(
        Guid tenantId,
        decimal amount,
        DateOnly expenseOn,
        string category,
        string? note,
        Guid? staffUserId,
        Guid actorId,
        CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        var result = await conn.QueryFirstAsync<dynamic>(
            "dbo.sp_expense_save",
            new
            {
                tenantid = tenantId,
                amount,
                expenseon = expenseOn.ToDateTime(TimeOnly.MinValue),
                category,
                note,
                staffuserid = staffUserId,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
        return (Guid)result.expenseid;
    }

    public async Task DeleteAsync(Guid tenantId, Guid expenseId, Guid actorId, CancellationToken ct)
    {
        using var conn = dbHelper.GetConnection();
        await conn.ExecuteAsync(
            "dbo.sp_expense_delete",
            new
            {
                tenantid = tenantId,
                expenseid = expenseId,
                actorid = actorId
            },
            commandType: CommandType.StoredProcedure);
    }
}
