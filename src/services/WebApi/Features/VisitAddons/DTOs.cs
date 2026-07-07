namespace WebApi.Features.VisitAddons;

public record CreateVisitAddonRequest(
    string Name,
    decimal DefaultAmount,
    string? Code = null);

public record UpdateVisitAddonRequest(
    string Name,
    decimal DefaultAmount,
    string? Code = null);

public record VisitAddonResponse(
    Guid Id,
    string Name,
    string? Code,
    decimal DefaultAmount,
    string Status,
    byte StatusCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record VisitAddonListResponse(
    IEnumerable<VisitAddonResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record VisitAddonLookupItem(
    Guid Id,
    string Name,
    string? Code,
    decimal DefaultAmount);

public record VisitAddonLineResponse(
    Guid Id,
    Guid VisitAddonId,
    string AddonName,
    decimal Amount,
    DateTime CreatedAt);
