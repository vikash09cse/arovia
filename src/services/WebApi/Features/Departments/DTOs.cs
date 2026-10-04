namespace WebApi.Features.Departments;

public record CreateDepartmentRequest(string Name);

public record UpdateDepartmentRequest(string Name);

public record DepartmentResponse(
    Guid Id,
    string Name,
    string Status,
    byte StatusCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DepartmentListResponse(
    IEnumerable<DepartmentResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record DepartmentLookupItem(
    Guid Id,
    string Name);
