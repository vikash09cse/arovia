using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Visits;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class VisitsController(VisitsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? patientId = null,
        [FromQuery] string? patientCode = null,
        [FromQuery] Guid? consultingDoctorId = null,
        [FromQuery] byte? visitType = null,
        [FromQuery] byte? feeStatus = null,
        [FromQuery] byte? visitStatus = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default) =>
        (await service.GetVisitsAsync(
            page, pageSize, patientId, patientCode, consultingDoctorId,
            visitType, feeStatus, visitStatus, dateFrom, dateTo, ct)).ToActionResult();

    [HttpGet("fee-preview")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> GetFeePreview([FromQuery] Guid patientId, CancellationToken ct) =>
        (await service.GetFeePreviewAsync(patientId, ct)).ToActionResult();

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors(CancellationToken ct) =>
        (await service.GetDoctorsAsync(ct)).ToActionResult();

    [HttpGet("payment-collectors")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> GetPaymentCollectors(CancellationToken ct) =>
        (await service.GetPaymentCollectorsAsync(ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Create([FromBody] CreateVisitRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/notes")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> UpdateNotes(Guid id, [FromBody] UpdateVisitNotesRequest request, CancellationToken ct) =>
        (await service.UpdateNotesAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/fee-override")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> OverrideFee(Guid id, [FromBody] FeeOverrideRequest request, CancellationToken ct) =>
        (await service.OverrideFeeAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/discount")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyDiscountRequest request, CancellationToken ct) =>
        (await service.ApplyDiscountAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelVisitRequest request, CancellationToken ct) =>
        (await service.CancelAsync(id, request, ct)).ToActionResult();
}
