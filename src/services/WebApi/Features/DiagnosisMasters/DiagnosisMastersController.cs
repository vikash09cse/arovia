using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.DiagnosisMasters;

[Route("api/diagnosis-masters")]
[ApiController]
public class DiagnosisMastersController(DiagnosisMastersService service) : ControllerBase
{
    [HttpGet("active")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetActive(CancellationToken ct) =>
        (await service.GetActiveAsync(ct)).ToActionResult();

    [HttpGet]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? filter = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default) =>
        (await service.GetListAsync(page, pageSize, filter, isActive, ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Create(
        [FromBody] SaveDiagnosisMasterRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] SaveDiagnosisMasterRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> SetStatus(
        Guid id, [FromQuery] bool isActive, CancellationToken ct) =>
        (await service.SetStatusAsync(id, isActive, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult();
}
