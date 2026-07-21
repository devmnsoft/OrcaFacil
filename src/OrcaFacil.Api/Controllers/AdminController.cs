using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[Authorize(Policy = "SuperAdmin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult Dashboard() => Ok(new { users = 0, documents = 0, errors = 0 });

    [HttpGet("users")]
    public IActionResult Users() => Ok(Array.Empty<object>());

    [HttpGet("logs")]
    public IActionResult Logs() => Ok(Array.Empty<object>());

    [HttpGet("errors")]
    public IActionResult Errors() => Ok(Array.Empty<object>());

    [HttpPost("errors/{id:guid}/resolve")]
    public IActionResult Resolve(Guid id) => Ok(new { id, resolved = true });

    [HttpGet("audit")]
    public IActionResult Audit() => Ok(Array.Empty<object>());

    [HttpPut("settings/{key}")]
    public IActionResult Settings(string key, object body) => Ok(new { key });
}
