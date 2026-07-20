using Microsoft.AspNetCore.Mvc; using OrcaFacil.Application.UseCases;
namespace OrcaFacil.Api.Controllers; [ApiController,Route("api/auth")]
public class AuthController(AuthService auth):ControllerBase{ [HttpPost("register")] public Task<OrcaFacil.Shared.Result<Guid>> Register(RegisterUserCommand c,CancellationToken ct)=>auth.RegisterAsync(c,ct); [HttpPost("login")] public OrcaFacil.Shared.Result<Guid> Login(LoginUserCommand c)=>auth.Login(c); [HttpPost("logout")] public IActionResult Logout()=>Ok(); [HttpGet("me")] public IActionResult Me()=>Ok(new{User.Identity?.Name}); }
