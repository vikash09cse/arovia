using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.UserDocuments;

[Route("api/users/{userId:guid}/documents")]
[ApiController]
[Authorize(Roles = RoleNames.TenantSuperAdmin)]
public class UserDocumentsController(UserDocumentsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(Guid userId, CancellationToken ct) =>
        (await service.GetListAsync(userId, ct)).ToActionResult();

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid userId, Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(userId, id, ct);
        if (!result.Success || result.Data is null)
            return result.ToActionResult();

        var data = result.Data;
        return File(data.Bytes, data.ContentType, data.DisplayName);
    }

    [HttpPost]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid userId,
        [FromForm] byte documentType,
        IFormFile file,
        CancellationToken ct) =>
        (await service.UploadAsync(userId, documentType, file, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(userId, id, ct)).ToActionResult();
}
