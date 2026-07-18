using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Dashboard;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default) =>
        (await service.GetDashboardAsync(dateFrom, dateTo, ct)).ToActionResult();
}
