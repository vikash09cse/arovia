using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.MonthlyReconcile;

[Route("api/monthly-reconcile")]
[ApiController]
[Authorize(Roles = RoleNames.TenantSuperAdmin)]
public class MonthlyReconcileController(MonthlyReconcileService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? yearMonth,
        CancellationToken ct) =>
        (await service.GetAsync(yearMonth, ct)).ToActionResult();

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct) =>
        (await service.GetHistoryAsync(ct)).ToActionResult();

    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile(
        [FromBody] ReconcileMonthRequest request,
        CancellationToken ct) =>
        (await service.ReconcileAsync(request, ct)).ToActionResult();

    [HttpPost("reopen")]
    public async Task<IActionResult> Reopen(
        [FromBody] ReopenMonthRequest request,
        CancellationToken ct) =>
        (await service.ReopenAsync(request, ct)).ToActionResult();
}
