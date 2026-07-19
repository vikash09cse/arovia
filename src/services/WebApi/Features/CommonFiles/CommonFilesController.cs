using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.CommonFiles;

[Route("api/common-files")]
[ApiController]
public class CommonFilesController(CommonFilesService service) : ControllerBase
{
    private const string AllTenantRoles =
        $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}";

    [HttpGet]
    [Authorize(Roles = AllTenantRoles)]
    public async Task<IActionResult> GetList(CancellationToken ct) =>
        (await service.GetListAsync(ct)).ToActionResult();

    [HttpGet("{id:guid}/download")]
    [Authorize(Roles = AllTenantRoles)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(id, ct);
        if (!result.Success || result.Data is null)
            return result.ToActionResult();

        var data = result.Data;
        return File(data.Bytes, data.ContentType, data.DisplayName);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct) =>
        (await service.UploadAsync(file, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.TenantSuperAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult();
}
