using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.DischargeSummaries;

[Route("api/diagnosis-masters")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class DiagnosisMastersController(DischargeSummariesService service) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct) =>
        (await service.GetActiveDiagnosesAsync(ct)).ToActionResult();
}
