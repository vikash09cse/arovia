using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Expenses;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = RoleNames.TenantSuperAdmin)]
public class ExpensesController(ExpensesService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByMonth(
        [FromQuery] string? yearMonth,
        CancellationToken ct) =>
        (await service.GetByMonthAsync(yearMonth, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult();
}
