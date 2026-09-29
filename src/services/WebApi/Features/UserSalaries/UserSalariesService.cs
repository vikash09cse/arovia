using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.UserSalaries.Infrastructure;
using WebApi.Features.Users.Infrastructure;

namespace WebApi.Features.UserSalaries;

public class UserSalariesService(
    IUserSalariesRepository repository,
    IUsersRepository usersRepository,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<Result<IEnumerable<UserSalaryResponse>>> GetListAsync(Guid userId, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<IEnumerable<UserSalaryResponse>>();
        if (tenantError != null) return tenantError;

        var tenantId = GetTenantId();
        var user = await usersRepository.GetByIdAsync(tenantId, userId, ct);
        if (user == null)
            return Result<IEnumerable<UserSalaryResponse>>.Fail(ErrorCode.NotFound, "User not found.");

        if (user.Role != (byte)UserType.Staff)
            return Result<IEnumerable<UserSalaryResponse>>.Fail(
                ErrorCode.Validation, "Monthly salary history is only available for Staff users.");

        var rows = await repository.GetListAsync(tenantId, userId, ct);
        return Result<IEnumerable<UserSalaryResponse>>.Ok(rows.Select(Map));
    }

    public async Task<Result<UserSalaryResponse>> AddAsync(
        Guid userId, CreateUserSalaryRequest request, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<UserSalaryResponse>();
        if (tenantError != null) return tenantError;

        if (request.MonthlySalary < 0)
            return Result<UserSalaryResponse>.Fail(ErrorCode.Validation, "Monthly salary cannot be negative.");

        var tenantId = GetTenantId();
        var user = await usersRepository.GetByIdAsync(tenantId, userId, ct);
        if (user == null)
            return Result<UserSalaryResponse>.Fail(ErrorCode.NotFound, "User not found.");

        if (user.Role != (byte)UserType.Staff)
            return Result<UserSalaryResponse>.Fail(
                ErrorCode.Validation, "Monthly salary can only be set for Staff users.");

        try
        {
            var row = await repository.AddAsync(
                tenantId,
                userId,
                request.MonthlySalary,
                request.EffectiveFrom,
                string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                GetUserId(),
                ct);
            return Result<UserSalaryResponse>.Ok(Map(row), "Salary saved.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<UserSalaryResponse>.Fail(ErrorCode.Validation, message);
        }
    }

    private static UserSalaryResponse Map(UserSalaryRow row) => new(
        row.UserSalaryId,
        row.UserId,
        row.MonthlySalary,
        DateOnly.FromDateTime(row.EffectiveFrom),
        row.EffectiveTo.HasValue ? DateOnly.FromDateTime(row.EffectiveTo.Value) : null,
        row.Notes,
        row.CreatedAt);

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

    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;
    private Guid GetUserId() => httpContextAccessor.GetTenantContext().UserId;
}
