using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Enums;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.UserSalaries;

[Route("api/users/{userId:guid}/salaries")]
[ApiController]
[Authorize(Roles = RoleNames.TenantSuperAdmin)]
public class UserSalariesController(UserSalariesService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(Guid userId, CancellationToken ct) =>
        (await service.GetListAsync(userId, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid userId, [FromBody] CreateUserSalaryRequest request, CancellationToken ct) =>
        (await service.AddAsync(userId, request, ct)).ToActionResult();
}
