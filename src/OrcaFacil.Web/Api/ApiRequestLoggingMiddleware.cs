using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Api;

public sealed class ApiRequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, OrcaFacilDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1")) { await next(context); return; }
        var timer = Stopwatch.StartNew();
        try { await next(context); }
        finally
        {
            if (Guid.TryParse(context.User.FindFirstValue("account_id"), out var accountId) && Guid.TryParse(context.User.FindFirstValue("api_key_id"), out var apiKeyId))
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();
                db.ApiRequestLogs.Add(new ApiRequestLog
                {
                    AccountId = accountId, ApiKeyId = apiKeyId, Route = context.Request.Path.Value?[..Math.Min(context.Request.Path.Value.Length, 300)] ?? "/api/v1",
                    Method = context.Request.Method, StatusCode = context.Response.StatusCode, ElapsedMilliseconds = timer.ElapsedMilliseconds,
                    IpAddress = string.IsNullOrEmpty(ip) ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ip))).ToLowerInvariant(),
                    UserAgent = Sanitize(context.Request.Headers.UserAgent.ToString()), CorrelationId = context.TraceIdentifier,
                    ErrorCode = context.Response.StatusCode >= 400 ? StatusCode(context.Response.StatusCode) : null
                });
                await db.SaveChangesAsync(context.RequestAborted);
            }
        }
    }
    private static string? Sanitize(string value) => string.IsNullOrWhiteSpace(value) ? null : new string(value.Where(c => !char.IsControl(c)).Take(300).ToArray());
    private static string StatusCode(int status) => status switch { 401 => "unauthorized", 403 => "forbidden", 404 => "not_found", 409 => "resource_conflict", 429 => "rate_limited", _ => status >= 500 ? "internal_error" : "validation_error" };
}
