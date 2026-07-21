using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OrcaFacil.Infrastructure;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[HeaderName] = id;
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await _next(context);
        }
    }
}
