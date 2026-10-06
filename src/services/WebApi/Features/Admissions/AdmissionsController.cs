using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Admissions;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class AdmissionsController(AdmissionsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] byte? status = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] string? admissionCode = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default) =>
        (await service.GetListAsync(page, pageSize, status, patientId, admissionCode, dateFrom, dateTo, ct))
            .ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Create([FromBody] CreateAdmissionRequest request, CancellationToken ct) =>
        (await service.CreateAsync(request, ct)).ToActionResult();

    [HttpPost("{id:guid}/charges")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> AddCharge(Guid id, [FromBody] AddAdmissionChargeRequest request, CancellationToken ct) =>
        (await service.AddChargeAsync(id, request, ct)).ToActionResult();

    [HttpPatch("{id:guid}/discount")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyAdmissionDiscountRequest request, CancellationToken ct) =>
        (await service.ApplyDiscountAsync(id, request, ct)).ToActionResult();

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddAdmissionPaymentRequest request, CancellationToken ct) =>
        (await service.AddPaymentAsync(id, request, ct)).ToActionResult();

    [HttpPost("{id:guid}/discharge")]
    [Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}")]
    public async Task<IActionResult> Discharge(Guid id, CancellationToken ct) =>
        (await service.DischargeAsync(id, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult();
}
