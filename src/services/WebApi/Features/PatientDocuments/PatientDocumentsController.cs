using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.PatientDocuments;

[Route("api/patients/{patientId:guid}/documents")]
[ApiController]
public class PatientDocumentsController(PatientDocumentsService service) : ControllerBase
{
    private const string AllTenantRoles =
        $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}";

    private const string ManageRoles =
        $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff}";

    [HttpGet]
    [Authorize(Roles = AllTenantRoles)]
    public async Task<IActionResult> GetList(Guid patientId, CancellationToken ct) =>
        (await service.GetListAsync(patientId, ct)).ToActionResult();

    [HttpGet("{id:guid}/download")]
    [Authorize(Roles = AllTenantRoles)]
    public async Task<IActionResult> Download(Guid patientId, Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(patientId, id, ct);
        if (!result.Success || result.Data is null)
            return result.ToActionResult();

        var data = result.Data;
        return File(data.Bytes, data.ContentType, data.DisplayName);
    }

    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid patientId, IFormFile file, CancellationToken ct) =>
        (await service.UploadAsync(patientId, file, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> Delete(Guid patientId, Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(patientId, id, ct)).ToActionResult();
}
