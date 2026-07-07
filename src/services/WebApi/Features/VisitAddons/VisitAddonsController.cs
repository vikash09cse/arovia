using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.VisitAddons;

[Route("api/visit-addons")]
[ApiController]
public class VisitAddonsController(VisitAddonsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
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

    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Create([FromBody] CreateVisitAddonRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVisitAddonRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> SetStatus(Guid id, [FromQuery] byte status, CancellationToken ct)
    {
        if (status is not ((byte)VisitAddonStatus.Active) and not ((byte)VisitAddonStatus.Inactive))
            return Result<bool>.Fail(ErrorCode.Validation, "Invalid status.").ToActionResult();

        return (await service.SetStatusAsync(id, (VisitAddonStatus)status, ct)).ToActionResult();
    }
}
