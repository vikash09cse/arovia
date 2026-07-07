using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Features.Reports;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController(ReportsService service) : ControllerBase;
