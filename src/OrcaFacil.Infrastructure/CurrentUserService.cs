using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId => TryGetUserId() ?? throw new UnauthorizedAccessException("Usuário não autenticado ou claim 'sub' inválida.");

    public string? Email => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
    public string? Name => User?.FindFirstValue(ClaimTypes.Name) ?? User?.FindFirstValue("name");
    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");
    public string? Plan => User?.FindFirstValue("plan");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public bool IsSuperAdmin => string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    public Guid? TryGetUserId()
    {
        var value = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
