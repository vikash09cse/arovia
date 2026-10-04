namespace WebApi.Features.Expenses;

public record CreateExpenseRequest(
    decimal Amount,
    DateOnly ExpenseOn,
    string Category,
    string? Note,
    Guid? StaffUserId);

public record ExpenseItemResponse(
    Guid Id,
    decimal Amount,
    DateOnly ExpenseOn,
    string Category,
    string? Note,
    Guid? StaffUserId,
    string? StaffName,
    DateTime CreatedAt);

public record ExpenseCategoryTotalResponse(
    string Category,
    decimal Total);

public record ExpenseMonthResponse(
    string YearMonth,
    bool IsReconciled,
    decimal MonthTotal,
    IEnumerable<ExpenseCategoryTotalResponse> CategoryBreakdown,
    IEnumerable<ExpenseItemResponse> Items);
