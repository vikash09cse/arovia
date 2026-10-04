using System.Text.Json;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.DiagnosisMasters.Infrastructure;

namespace WebApi.Features.DiagnosisMasters;

public class DiagnosisMastersService(
    IDiagnosisMastersRepository repository,
    IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<Result<DiagnosisMasterActiveListResponse>> GetActiveAsync(CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DiagnosisMasterActiveListResponse>();
        if (tenantError != null) return tenantError;

        var rows = await repository.GetActiveAsync(GetTenantId(), ct);
        var items = rows.Select(r => new DiagnosisMasterActiveItem(
            r.DiagnosisMasterId, r.Code, r.Name, ParsePack(r.PackJson)));
        return Result<DiagnosisMasterActiveListResponse>.Ok(new DiagnosisMasterActiveListResponse(items));
    }

    public async Task<Result<DiagnosisMasterAdminListResponse>> GetListAsync(
        int page, int pageSize, string? filter, bool? isActive, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DiagnosisMasterAdminListResponse>();
        if (tenantError != null) return tenantError;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, total) = await repository.GetListAsync(
            GetTenantId(), page, pageSize,
            string.IsNullOrWhiteSpace(filter) ? null : filter.Trim(),
            isActive, ct);

        return Result<DiagnosisMasterAdminListResponse>.Ok(new DiagnosisMasterAdminListResponse(
            items.Select(MapAdminItem), total, page, pageSize));
    }

    public async Task<Result<DiagnosisMasterDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<DiagnosisMasterDetailResponse>();
        if (tenantError != null) return tenantError;

        var row = await repository.GetByIdAsync(GetTenantId(), id, ct);
        if (row == null)
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.NotFound, "Diagnosis not found.");

        return Result<DiagnosisMasterDetailResponse>.Ok(MapDetail(row));
    }

    public async Task<Result<DiagnosisMasterDetailResponse>> CreateAsync(
        SaveDiagnosisMasterRequest request, CancellationToken ct)
    {
        var validation = ValidateSave(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<DiagnosisMasterDetailResponse>();
        if (tenantError != null) return tenantError;

        var packJson = SerializePack(request.Pack);
        try
        {
            var id = await repository.SaveAsync(
                GetTenantId(), null, null, request.Name.Trim(), packJson,
                request.SortOrder, request.IsActive, ct);
            var created = await repository.GetByIdAsync(GetTenantId(), id, ct);
            return Result<DiagnosisMasterDetailResponse>.Ok(MapDetail(created!), "Diagnosis created.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<DiagnosisMasterDetailResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<DiagnosisMasterDetailResponse>> UpdateAsync(
        Guid id, SaveDiagnosisMasterRequest request, CancellationToken ct)
    {
        var validation = ValidateSave(request);
        if (validation != null) return validation;

        var tenantError = RequireTenantContext<DiagnosisMasterDetailResponse>();
        if (tenantError != null) return tenantError;

        var existing = await repository.GetByIdAsync(GetTenantId(), id, ct);
        if (existing == null)
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.NotFound, "Diagnosis not found.");

        var packJson = SerializePack(request.Pack);
        try
        {
            await repository.SaveAsync(
                GetTenantId(), id, existing.Code, request.Name.Trim(), packJson,
                request.SortOrder, request.IsActive, ct);
            var updated = await repository.GetByIdAsync(GetTenantId(), id, ct);
            return Result<DiagnosisMasterDetailResponse>.Ok(MapDetail(updated!), "Diagnosis updated.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<DiagnosisMasterDetailResponse>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<bool>> SetStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        try
        {
            await repository.SetStatusAsync(GetTenantId(), id, isActive, ct);
            return Result<bool>.Ok(true, isActive ? "Diagnosis activated." : "Diagnosis deactivated.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<bool>.Fail(MapSqlError(ex), message);
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var tenantError = RequireTenantContext<bool>();
        if (tenantError != null) return tenantError;

        try
        {
            await repository.DeleteAsync(GetTenantId(), id, ct);
            return Result<bool>.Ok(true, "Diagnosis deleted.");
        }
        catch (Exception ex) when (TrySqlMessage(ex, out var message))
        {
            return Result<bool>.Fail(MapSqlError(ex), message);
        }
    }

    private static Result<DiagnosisMasterDetailResponse>? ValidateSave(SaveDiagnosisMasterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.Validation, "Diagnosis name is required.");
        if (request.Name.Trim().Length > 300)
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.Validation, "Diagnosis name cannot exceed 300 characters.");
        if (request.Pack.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.Validation, "Pack payload is required.");
        if (request.Pack.ValueKind != JsonValueKind.Object)
            return Result<DiagnosisMasterDetailResponse>.Fail(ErrorCode.Validation, "Pack must be a JSON object.");
        return null;
    }

    private static DiagnosisMasterAdminItem MapAdminItem(DiagnosisMasterAdminRow row)
    {
        var pack = ParsePack(row.PackJson);
        string? procedureKey = null;
        string? followUpKey = null;
        if (pack.ValueKind == JsonValueKind.Object)
        {
            if (pack.TryGetProperty("procedureKey", out var pk))
                procedureKey = pk.GetString();
            if (pack.TryGetProperty("followUpKey", out var fk))
                followUpKey = fk.GetString();
        }

        return new DiagnosisMasterAdminItem(
            row.DiagnosisMasterId,
            row.Code,
            row.Name,
            string.IsNullOrWhiteSpace(procedureKey) ? null : procedureKey,
            string.IsNullOrWhiteSpace(followUpKey) ? null : followUpKey,
            row.IsActive,
            row.SortOrder,
            row.UpdatedAt);
    }

    private static DiagnosisMasterDetailResponse MapDetail(DiagnosisMasterAdminRow row) => new(
        row.DiagnosisMasterId,
        row.Code,
        row.Name,
        ParsePack(row.PackJson),
        row.IsActive,
        row.SortOrder,
        row.CreatedAt,
        row.UpdatedAt);

    private static string SerializePack(JsonElement pack) =>
        JsonSerializer.Serialize(pack, JsonOptions);

    private static JsonElement ParsePack(string? packJson)
    {
        if (string.IsNullOrWhiteSpace(packJson))
            return JsonDocument.Parse("{}").RootElement.Clone();
        return JsonDocument.Parse(packJson).RootElement.Clone();
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

    private Guid GetTenantId() => httpContextAccessor.GetTenantContext().TenantId;
}
