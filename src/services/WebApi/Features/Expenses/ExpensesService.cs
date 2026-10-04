using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Expenses.Infrastructure;

namespace WebApi.Features.Expenses;

public class ExpensesService(
    IExpensesRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Rent", "Salaries", "Utilities", "Supplies", "Staff advance", "Other"
    };

    public async Task<Result<ExpenseMonthResponse>> GetByMonthAsync(string? yearMonth, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<ExpenseMonthResponse>();
        if (tenantError != null) return tenantError;

        var ym = NormalizeYearMonth(yearMonth);
        if (ym == null)
            return Result<ExpenseMonthResponse>.Fail(ErrorCode.Validation, "Invalid year-month. Use YYYY-MM.");

        try
        {
            var (summary, categories, items) = await repository.GetByMonthAsync(GetTenantId(), ym, ct);
            return Result<ExpenseMonthResponse>.Ok(new ExpenseMonthResponse(
                summary.YearMonth.Trim(),
                summary.IsReconciled,
                summary.MonthTotal,
                categories.Select(c => new ExpenseCategoryTotalResponse(c.Category, c.Total)),
                items.Select(MapItem)));
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<ExpenseMonthResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<ExpenseItemResponse>> CreateAsync(CreateExpenseRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<ExpenseItemResponse>();
        if (tenantError != null) return tenantError;

        if (request.Amount <= 0)
            return Result<ExpenseItemResponse>.Fail(ErrorCode.Validation, "Amount must be greater than 0.");

        var category = request.Category?.Trim() ?? "";
        if (!AllowedCategories.Contains(category))
            return Result<ExpenseItemResponse>.Fail(ErrorCode.Validation, "Invalid expense category.");

        // Canonical casing
        category = AllowedCategories.First(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (category.Equals("Staff advance", StringComparison.OrdinalIgnoreCase) && request.StaffUserId is null)
            return Result<ExpenseItemResponse>.Fail(ErrorCode.Validation, "Select a staff member for the advance.");

        try
        {
            var id = await repository.SaveAsync(
                GetTenantId(),
                request.Amount,
                request.ExpenseOn,
                category,
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                category.Equals("Staff advance", StringComparison.OrdinalIgnoreCase) ? request.StaffUserId : null,
                GetUserId(),
                ct);

            var ym = $"{request.ExpenseOn.Year:D4}-{request.ExpenseOn.Month:D2}";
            var (_, _, items) = await repository.GetByMonthAsync(GetTenantId(), ym, ct);
            var created = items.FirstOrDefault(i => i.ExpenseId == id);
            if (created == null)
                return Result<ExpenseItemResponse>.Ok(new ExpenseItemResponse(
                    id, request.Amount, request.ExpenseOn, category,
                    string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                    request.StaffUserId, null, DateTime.UtcNow), "Expense added.");

            return Result<ExpenseItemResponse>.Ok(MapItem(created), "Expense added.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<ExpenseItemResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid expenseId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        try
        {
            await repository.DeleteAsync(GetTenantId(), expenseId, GetUserId(), ct);
            return Result<bool>.Ok(true, "Expense deleted.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<bool>.Fail(MapSqlError(ex), message);
        }
    }

    private static ExpenseItemResponse MapItem(ExpenseRow row) => new(
        row.ExpenseId,
        row.Amount,
        DateOnly.FromDateTime(row.ExpenseOn),
        row.Category,
        row.Note,
        row.StaffUserId,
        string.IsNullOrWhiteSpace(row.StaffName) ? null : row.StaffName.Trim(),
        row.CreatedAt);

    private static string? NormalizeYearMonth(string? yearMonth)
    {
        var value = string.IsNullOrWhiteSpace(yearMonth)
            ? $"{DateTime.UtcNow:yyyy-MM}"
            : yearMonth.Trim();

        if (value.Length != 7 || value[4] != '-') return null;
        if (!int.TryParse(value.AsSpan(0, 4), out var year) || year < 2000 || year > 2100) return null;
        if (!int.TryParse(value.AsSpan(5, 2), out var month) || month is < 1 or > 12) return null;
        return $"{year:D4}-{month:D2}";
    }

    private static ErrorCode MapSqlError(Exception ex)
    {
        if (ex is Microsoft.Data.SqlClient.SqlException sql)
        {
            return sql.Number switch
            {
                50404 => ErrorCode.NotFound,
                50409 => ErrorCode.AlreadyExists,
                _ => ErrorCode.Validation
            };
        }
        return ErrorCode.Validation;
    }

    private static bool TrySqlMessage(Exception ex, out string message)
    {
        message = ex.Message;
        if (ex is Microsoft.Data.SqlClient.SqlException sql && !string.IsNullOrWhiteSpace(sql.Message))
        {
            message = sql.Message.Split('\n')[0].Trim();
            return true;
        }

        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner is Microsoft.Data.SqlClient.SqlException sqlInner && !string.IsNullOrWhiteSpace(sqlInner.Message))
            {
                message = sqlInner.Message.Split('\n')[0].Trim();
                return true;
            }
        }

        return false;
    }

    private Result<T>? RequireTenantContext<T>()
    {
        var ctx = httpContextAccessor.HttpContext?.TryGetTenantContext();
        if (ctx == null || !ctx.IsValidForTenantScope())
            return Result<T>.Fail(ErrorCode.Forbidden, "Tenant context is required.");
        return null;
    }

    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;
}
