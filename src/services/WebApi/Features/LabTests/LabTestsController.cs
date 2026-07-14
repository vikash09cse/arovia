using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.LabTests;

[Route("api/lab-agencies")]
[ApiController]
public class LabTestsController(LabTestsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filter = null,
        [FromQuery] byte? status = null,
        CancellationToken ct = default) =>
        (await service.GetListAsync(page, pageSize, filter, status, ct)).ToActionResult();

    [HttpGet("active")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetActive(CancellationToken ct) =>
        (await service.GetActiveAsync(ct)).ToActionResult();

    [HttpGet("assignment-report")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetAssignmentReport(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? patientCode = null,
        CancellationToken ct = default) =>
        (await service.GetAssignmentReportAsync(dateFrom, dateTo, phone, patientCode, ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateLabAgencyRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLabAgencyRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> SetStatus(Guid id, [FromQuery] byte status, CancellationToken ct)
    {
        if (status is not ((byte)LabAgencyStatus.Active) and not ((byte)LabAgencyStatus.Inactive))
            return Result<bool>.Fail(ErrorCode.Validation, "Invalid status.").ToActionResult();

        return (await service.SetStatusAsync(id, (LabAgencyStatus)status, ct)).ToActionResult();
    }

    [HttpPost("/api/visits/{visitId:guid}/lab-agencies")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> AssignToVisit(
        Guid visitId, [FromBody] AssignVisitLabAgencyRequest request, CancellationToken ct) =>
        (await service.AssignToVisitAsync(visitId, request, ct)).ToActionResult();

    [HttpDelete("/api/visits/{visitId:guid}/lab-agencies/{assignmentId:guid}")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> RemoveFromVisit(
        Guid visitId, Guid assignmentId, CancellationToken ct) =>
        (await service.RemoveFromVisitAsync(visitId, assignmentId, ct)).ToActionResult();
}
