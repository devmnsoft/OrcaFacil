using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Profile;
using OrcaFacil.Shared;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profiles;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(ProfileService profiles, ICurrentUserService currentUser, ILogger<ProfileController> logger)
    {
        _profiles = profiles;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _profiles.GetAsync(new GetIssuerProfileQuery(_currentUser.UserId), ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Result<Guid>>> Save(SaveIssuerProfileCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _profiles.SaveAsync(command with { UserId = _currentUser.UserId }, ct);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar perfil do emitente");
            throw;
        }
    }
}
