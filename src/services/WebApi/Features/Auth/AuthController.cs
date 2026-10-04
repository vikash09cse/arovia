using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Utilities;
using SharedKernel.Utilities.Extensions;

namespace WebApi.Features.Auth;

[Route("api/[controller]")]
[ApiController]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpGet("tenant-by-subdomain/{subdomain}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTenantBySubdomain(string subdomain, CancellationToken ct) =>
        (await authService.GetTenantBySubdomainAsync(subdomain, ct)).ToActionResult();

    [HttpPost("platform-login")]
    [AllowAnonymous]
    public async Task<IActionResult> PlatformLogin([FromBody] PlatformLoginRequest request, CancellationToken ct) =>
        (await authService.PlatformLoginAsync(request, ct)).ToActionResult();

    [HttpPost("tenant-login")]
    [AllowAnonymous]
    public async Task<IActionResult> TenantLogin([FromBody] TenantLoginRequest request, CancellationToken ct) =>
        (await authService.TenantLoginAsync(request, ct)).ToActionResult();

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Result<LoginResponse>.Fail(ErrorCode.Unauthorized, "Authorization header with Bearer token is required.").ToActionResult();

        var token = authHeader["Bearer ".Length..].Trim();
        return (await authService.RefreshTokenAsync(token, ct)).ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct) =>
        (await authService.LogoutAsync(ct)).ToActionResult();

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct) =>
        (await authService.GetMyProfileAsync(ct)).ToActionResult();

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request, CancellationToken ct) =>
        (await authService.UpdateMyProfileAsync(request, ct)).ToActionResult();

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken ct) =>
        (await authService.ChangePasswordAsync(request, ct)).ToActionResult();
}
