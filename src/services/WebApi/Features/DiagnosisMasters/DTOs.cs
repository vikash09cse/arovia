using System.Text.Json;

namespace WebApi.Features.DiagnosisMasters;

public record SaveDiagnosisMasterRequest(
    string Name,
    JsonElement Pack,
    int SortOrder = 0,
    bool IsActive = true);

public record DiagnosisMasterAdminItem(
    Guid Id,
    string Code,
    string Name,
    string? ProcedureKey,
    string? FollowUpKey,
    bool IsActive,
    int SortOrder,
    DateTime UpdatedAt);

public record DiagnosisMasterAdminListResponse(
    IEnumerable<DiagnosisMasterAdminItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DiagnosisMasterDetailResponse(
    Guid Id,
    string Code,
    string Name,
    JsonElement Pack,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DiagnosisMasterActiveItem(
    Guid Id,
    string Code,
    string Name,
    JsonElement Pack);

public record DiagnosisMasterActiveListResponse(
    IEnumerable<DiagnosisMasterActiveItem> Items);
