namespace WebApi.Features.Expenses.Infrastructure;

public class ExpenseMonthSummaryRow
{
    public string YearMonth { get; set; } = string.Empty;
    public bool IsReconciled { get; set; }
    public decimal MonthTotal { get; set; }
}

public class ExpenseCategoryTotalRow
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ExpenseRow
{
    public Guid ExpenseId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseOn { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Note { get; set; }
    public Guid? StaffUserId { get; set; }
    public string? StaffName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IExpensesRepository
{
    Task<(ExpenseMonthSummaryRow Summary, IReadOnlyList<ExpenseCategoryTotalRow> Categories, IReadOnlyList<ExpenseRow> Items)>
        GetByMonthAsync(Guid tenantId, string yearMonth, CancellationToken ct);

    Task<Guid> SaveAsync(
        Guid tenantId,
        decimal amount,
        DateOnly expenseOn,
        string category,
        string? note,
        Guid? staffUserId,
        Guid actorId,
        CancellationToken ct);

    Task DeleteAsync(Guid tenantId, Guid expenseId, Guid actorId, CancellationToken ct);
}
