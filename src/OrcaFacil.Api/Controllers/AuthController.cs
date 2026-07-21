using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcaFacil.Application.Auth;
using OrcaFacil.Shared;

namespace OrcaFacil.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string Scheme = "OrcaCookie";
    private readonly AuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<Result<Guid>>> Register(RegisterUserCommand command, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Registro iniciado para {Email}", command.Email);
            var result = await _auth.RegisterAsync(command, ct);
            if (!result.Succeeded || result.Value is null) return BadRequest(result);
            return Ok(Result<Guid>.Ok(result.Value.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário");
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(command, ct);
            if (!result.Succeeded || result.Value is null) return BadRequest(result);
            var user = result.Value;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("sub", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("plan", user.Plan),
            };
            await HttpContext.SignInAsync(Scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar login");
            throw;
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(Scheme);
        _logger.LogInformation("USER_LOGOUT {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
        return Ok(Result.Ok());
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { User.Identity?.Name });
}
