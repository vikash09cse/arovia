using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.DischargeSummaries;

[Route("api/admissions/{admissionId:guid}/discharge-summary")]
[ApiController]
[Authorize(Roles = $"{RoleNames.TenantSuperAdmin},{RoleNames.Staff},{RoleNames.Doctor}")]
public class DischargeSummariesController(DischargeSummariesService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid admissionId, CancellationToken ct) =>
        (await service.GetAsync(admissionId, ct)).ToActionResult();

    [HttpPut]
    public async Task<IActionResult> Save(
        Guid admissionId,
        [FromBody] SaveDischargeSummaryRequest request,
        CancellationToken ct) =>
        (await service.SaveAsync(admissionId, request, ct)).ToActionResult();

    [HttpGet("print")]
    public async Task<IActionResult> Print(Guid admissionId, CancellationToken ct) =>
        (await service.GetPrintHtmlAsync(admissionId, ct)).ToActionResult();

    [HttpGet("print.pdf")]
    public async Task<IActionResult> PrintPdf(Guid admissionId, CancellationToken ct)
    {
        var result = await service.GetPrintPdfAsync(admissionId, ct);
        if (!result.Success || result.Data is null)
            return result.ToActionResult();

        var code = result.Data.AdmissionCode;
        var safeName = string.IsNullOrWhiteSpace(code) ? admissionId.ToString("N") : code;
        return File(result.Data.Bytes, "application/pdf", $"discharge-summary-{safeName}.pdf");
    }
}
