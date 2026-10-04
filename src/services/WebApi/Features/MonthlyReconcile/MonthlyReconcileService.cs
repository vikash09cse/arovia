using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.MonthlyReconcile.Infrastructure;

namespace WebApi.Features.MonthlyReconcile;

public class MonthlyReconcileService(
    IMonthlyReconcileRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<Result<MonthlyReconcileResponse>> GetAsync(string? yearMonth, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<MonthlyReconcileResponse>();
        if (tenantError != null) return tenantError;

        var ym = NormalizeYearMonth(yearMonth);
        if (ym == null)
            return Result<MonthlyReconcileResponse>.Fail(ErrorCode.Validation, "Invalid year-month. Use YYYY-MM.");

        try
        {
            var (profile, preview) = await repository.GetAsync(GetTenantId(), ym, ct);
            var isReconciled = profile is { ProfileStatus: (byte)MonthlyProfileStatus.Reconciled };
            var totals = isReconciled && profile != null
                ? new MonthlyTotalsResponse(
                    profile.OpdRevenue, profile.IpdRevenue, profile.TotalRevenue,
                    profile.TotalExpenses, profile.Profit)
                : new MonthlyTotalsResponse(
                    preview.OpdRevenue, preview.IpdRevenue, preview.TotalRevenue,
                    preview.TotalExpenses, preview.Profit);

            return Result<MonthlyReconcileResponse>.Ok(new MonthlyReconcileResponse(
                ym,
                isReconciled,
                totals,
                !isReconciled,
                profile == null ? null : MapProfile(profile)));
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<MonthlyReconcileResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<IEnumerable<MonthlyProfileResponse>>> GetHistoryAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<MonthlyProfileResponse>>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetHistoryAsync(GetTenantId(), ct);
        return Result<IEnumerable<MonthlyProfileResponse>>.Ok(rows.Select(MapProfile));
    }

    public async Task<Result<MonthlyProfileResponse>> ReconcileAsync(
        ReconcileMonthRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<MonthlyProfileResponse>();
        if (tenantError != null) return tenantError;

        var ym = NormalizeYearMonth(request.YearMonth);
        if (ym == null)
            return Result<MonthlyProfileResponse>.Fail(ErrorCode.Validation, "Invalid year-month. Use YYYY-MM.");

        try
        {
            var row = await repository.ReconcileAsync(
                GetTenantId(),
                ym,
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                GetUserId(),
                ct);
            return Result<MonthlyProfileResponse>.Ok(MapProfile(row), "Month reconciled.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<MonthlyProfileResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<bool>> ReopenAsync(ReopenMonthRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        var ym = NormalizeYearMonth(request.YearMonth);
        if (ym == null)
            return Result<bool>.Fail(ErrorCode.Validation, "Invalid year-month. Use YYYY-MM.");

        try
        {
            await repository.ReopenAsync(GetTenantId(), ym, GetUserId(), ct);
            return Result<bool>.Ok(true, "Month reopened.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<bool>.Fail(MapSqlError(ex), message);
        }
    }

    private static MonthlyProfileResponse MapProfile(MonthlyProfileRow row)
    {
        var status = row.ProfileStatus == (byte)MonthlyProfileStatus.Reconciled ? "reconciled" : "open";
        return new MonthlyProfileResponse(
            row.MonthlyProfileId,
            row.YearMonth.Trim(),
            status,
            row.ProfileStatus,
            row.OpdRevenue,
            row.IpdRevenue,
            row.TotalRevenue,
            row.TotalExpenses,
            row.Profit,
            row.Note,
            row.ReconciledAt,
            row.ReconciledBy,
            string.IsNullOrWhiteSpace(row.ReconcilerName) ? null : row.ReconcilerName.Trim(),
            row.CreatedAt);
    }

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
