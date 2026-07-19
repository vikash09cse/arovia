using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.TenantSettings;

[Route("api/tenant-settings")]
[ApiController]
[Authorize(Roles = RoleNames.TenantSuperAdmin)]
public class TenantSettingsController(TenantSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        (await service.GetAsync(ct)).ToActionResult();

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantSettingsRequest request, CancellationToken ct) =>
        (await service.UpdateAsync(request, ct)).ToActionResult();

    [HttpPost("logo")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct) =>
        (await service.UploadLogoAsync(file, ct)).ToActionResult();
}
