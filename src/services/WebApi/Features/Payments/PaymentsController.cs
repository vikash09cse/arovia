using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Payments;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(PaymentsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
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

    [HttpGet("pending")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? patientCode = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default) =>
        (await service.GetPendingVisitsAsync(
            page, pageSize, patientCode, dateFrom, dateTo, ct)).ToActionResult();

    [HttpGet("{id:guid}/receipt")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetReceiptHtml(Guid id, CancellationToken ct) =>
        (await service.GetReceiptHtmlAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/receipt.pdf")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
    public async Task<IActionResult> GetReceiptPdf(Guid id, CancellationToken ct)
    {
        var result = await service.GetReceiptPdfAsync(id, ct);
        if (!result.Success)
            return result.ToActionResult();

        var receiptNumber = result.Data.ReceiptNumber;
        var safeName = string.IsNullOrWhiteSpace(receiptNumber) ? id.ToString("N") : receiptNumber;
        return File(result.Data.Bytes, "application/pdf", $"{safeName}.pdf");
    }

    [HttpPost("/api/visits/{visitId:guid}/payments")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> AddCollection(
        Guid visitId, [FromBody] AddPaymentRequest request, CancellationToken ct) =>
        (await service.AddCollectionAsync(visitId, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/void")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> VoidCollection(
        Guid id, [FromBody] VoidPaymentRequest? request, CancellationToken ct) =>
        (await service.VoidCollectionAsync(id, request ?? new VoidPaymentRequest(), ct)).ToActionResult();

    [HttpPatch("visit/{visitId:guid}/collect-pending")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> CollectVisitPending(
        Guid visitId, [FromBody] CollectVisitPendingRequest? request, CancellationToken ct) =>
        (await service.CollectVisitPendingAsync(visitId, request, ct)).ToActionResult();
}
