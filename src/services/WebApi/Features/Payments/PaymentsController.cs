using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Payments;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
public class PaymentsController(PaymentsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? patientCode = null,
        [FromQuery] bool openVisitsOnly = false,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default) =>
        (await service.GetListAsync(
            page, pageSize, patientCode, openVisitsOnly, dateFrom, dateTo, ct)).ToActionResult();

    [HttpPost("/api/visits/{visitId:guid}/payments")]
    public async Task<IActionResult> AddCollection(
        Guid visitId, [FromBody] AddPaymentRequest request, CancellationToken ct) =>
        (await service.AddCollectionAsync(visitId, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/void")]
    public async Task<IActionResult> VoidCollection(
        Guid id, [FromBody] VoidPaymentRequest? request, CancellationToken ct) =>
        (await service.VoidCollectionAsync(id, request ?? new VoidPaymentRequest(), ct)).ToActionResult();

    [HttpPatch("visit/{visitId:guid}/collect-pending")]
    public async Task<IActionResult> CollectVisitPending(
        Guid visitId, [FromBody] CollectVisitPendingRequest? request, CancellationToken ct) =>
        (await service.CollectVisitPendingAsync(visitId, request, ct)).ToActionResult();
}
