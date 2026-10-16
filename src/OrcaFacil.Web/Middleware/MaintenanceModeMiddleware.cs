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
            <meta name="robots" content="noindex"><title>Fechô em manutenção</title>
            <style>body{margin:0;background:#f5f8f7;color:#172b2a;font:16px Inter,system-ui,sans-serif;display:grid;min-height:100vh;place-items:center}.card{width:min(90%,38rem);background:#fff;border-radius:24px;padding:clamp(2rem,6vw,4rem);box-shadow:0 24px 60px #0b38351f;text-align:center}h1{font-size:clamp(1.8rem,6vw,2.8rem);margin:.5rem 0}p{line-height:1.65;color:#536b67}.mark{font-weight:800;letter-spacing:.12em;color:#0f766e}.id{font-size:.8rem;margin-top:2rem}</style></head>
            <body><main class="card"><div class="mark">FECHÔ</div><h1>Manutenção em andamento</h1><p>Estamos realizando uma manutenção programada para manter o serviço seguro e estável. Aguarde alguns minutos e tente novamente.</p><p class="id">Identificador: <code>{{correlationId}}</code></p></main></body></html>
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
