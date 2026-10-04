namespace WebApi.Features.MonthlyReconcile;

public record ReconcileMonthRequest(string YearMonth, string? Note);

public record ReopenMonthRequest(string YearMonth);

public record MonthlyTotalsResponse(
    decimal OpdRevenue,
    decimal IpdRevenue,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal Profit);

public record MonthlyProfileResponse(
    Guid Id,
    string YearMonth,
    string Status,
    byte StatusCode,
    decimal OpdRevenue,
    decimal IpdRevenue,
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal Profit,
    string? Note,
    DateTime? ReconciledAt,
    Guid? ReconciledBy,
    string? ReconcilerName,
    DateTime CreatedAt);

public record MonthlyReconcileResponse(
    string YearMonth,
    bool IsReconciled,
    MonthlyTotalsResponse Totals,
    bool IsLive,
    MonthlyProfileResponse? Profile);
