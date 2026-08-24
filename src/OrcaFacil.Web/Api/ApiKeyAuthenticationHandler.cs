using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Api;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    OrcaFacilDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "ApiKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer of_live_", StringComparison.Ordinal))
            return AuthenticateResult.NoResult();

        var raw = authorization["Bearer ".Length..].Trim();
        if (raw.Length is < 40 or > 100) return AuthenticateResult.Fail("invalid_api_key");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        var key = await db.ApiKeys.SingleOrDefaultAsync(x => x.KeyHash == hash && !x.IsDeleted, Context.RequestAborted);
        if (key is null || key.RevokedAt.HasValue || (key.ExpiresAt.HasValue && key.ExpiresAt <= DateTime.UtcNow))
            return AuthenticateResult.Fail("invalid_api_key");

        key.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(Context.RequestAborted);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, key.Id.ToString()), new("api_key_id", key.Id.ToString()),
            new("account_id", key.AccountId.ToString()), new("api_key_prefix", key.Prefix)
        };
        claims.AddRange(key.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => new Claim("scope", x)));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)), Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => WriteError(StatusCodes.Status401Unauthorized, "unauthorized", "Forneça uma API key válida no cabeçalho Authorization.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => WriteError(StatusCodes.Status403Forbidden, "forbidden", "A API key não permite esta operação.");

    private Task WriteError(int status, string code, string message)
    {
        Response.StatusCode = status;
        Response.ContentType = "application/json";
        return Response.WriteAsJsonAsync(new { error = new { code, message, correlationId = Context.TraceIdentifier, details = Array.Empty<object>() } });
    }
}
