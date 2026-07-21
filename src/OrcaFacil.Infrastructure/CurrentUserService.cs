using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrcaFacil.Application.Abstractions;

namespace OrcaFacil.Infrastructure;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public Guid UserId => TryGetUserId() ?? throw new UnauthorizedAccessException("Usuário autenticado sem claim de identificador válida.");

    public Guid? TryGetUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
