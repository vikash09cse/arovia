namespace WebApi.Features.LabTests;

public record CreateLabAgencyRequest(
    string Name,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Notes = null);

public record UpdateLabAgencyRequest(
    string Name,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    string? Notes = null);

public record LabAgencyResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    string Status,
    byte StatusCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record LabAgencyListResponse(
    IEnumerable<LabAgencyResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record LabAgencyLookupItem(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Phone);

public record AssignVisitLabAgencyRequest(
    Guid LabAgencyId,
    string? TestName = null,
    string? Notes = null);

public record VisitLabAgencyResponse(
    Guid Id,
    Guid LabAgencyId,
    string AgencyName,
    DateTime AssignedAt,
    Guid AssignedByUserId,
    string? AssignedByName,
    string? TestName,
    string? Notes);

public record LabAgencyAssignmentReportItem(
    Guid Id,
    string Name,
    string? ContactPerson,
    string? Phone,
    string Status,
    byte StatusCode,
    int VisitCount);

public record LabAgencyAssignmentReportResponse(
    IEnumerable<LabAgencyAssignmentReportItem> Items,
    int TotalVisitAssignments);
