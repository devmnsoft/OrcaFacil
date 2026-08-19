using System.Net;

namespace OrcaFacil.Web.Middleware;

/// <summary>Returns a real maintenance response without hiding health or administrative diagnostics.</summary>
public sealed class MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!configuration.GetValue("MaintenanceMode:Enabled", false) || IsExempt(context))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "300";
        context.Response.ContentType = "text/html; charset=utf-8";
        var correlationId = WebUtility.HtmlEncode(context.TraceIdentifier);
        await context.Response.WriteAsync($$"""
            <!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="robots" content="noindex"><title>OrçaFácil em manutenção</title>
            <style>body{margin:0;background:#f4f7fb;color:#172033;font:16px system-ui,sans-serif;display:grid;min-height:100vh;place-items:center}.card{width:min(90%,38rem);background:#fff;border-radius:24px;padding:clamp(2rem,6vw,4rem);box-shadow:0 24px 60px #1720331f;text-align:center}h1{font-size:clamp(1.8rem,6vw,2.8rem);margin:.5rem 0}p{line-height:1.65;color:#556176}.mark{font-weight:800;color:#3157d5}.id{font-size:.8rem;margin-top:2rem}</style></head>
            <body><main class="card"><div class="mark">ORÇAFÁCIL</div><h1>Manutenção em andamento</h1><p>Estamos realizando uma manutenção programada para manter o serviço seguro e estável. Aguarde alguns minutos e tente novamente.</p><p class="id">Identificador: <code>{{correlationId}}</code></p></main></body></html>
            """);
    }

    private static bool IsExempt(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/css") ||
            path.StartsWithSegments("/js") || path.StartsWithSegments("/images") ||
            path.StartsWithSegments("/favicon")) return true;

        return context.User.Identity?.IsAuthenticated == true &&
               (context.User.IsInRole("SuperAdministrator") || context.User.IsInRole("SuperAdmin")) &&
               (path.StartsWithSegments("/Admin/SystemHealth") || path.StartsWithSegments("/Diagnostico"));
    }
}
