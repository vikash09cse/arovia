using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;
using WebApi.Features.Visits;

namespace WebApi.Features.Patients;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class PatientsController(PatientsService service, VisitsService visitsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? patientCode = null,
        [FromQuery] string? phone = null,
        [FromQuery] byte? status = null,
        [FromQuery] byte? gender = null,
        CancellationToken ct = default) =>
        (await service.GetPatientsAsync(page, pageSize, patientCode, phone, status, gender, ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/visit-summary")]
    public async Task<IActionResult> GetVisitSummary(Guid id, CancellationToken ct) =>
        (await visitsService.GetPatientSummaryAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Create([FromBody] SavePatientRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SavePatientRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> SetStatus(Guid id, [FromQuery] byte status, CancellationToken ct)
    {
        if (status is not ((byte)PatientStatus.Active) and not ((byte)PatientStatus.Inactive))
            return Result<bool>.Fail(ErrorCode.Validation, "Invalid status.").ToActionResult();

        return (await service.SetStatusAsync(id, (PatientStatus)status, ct)).ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult();
}
