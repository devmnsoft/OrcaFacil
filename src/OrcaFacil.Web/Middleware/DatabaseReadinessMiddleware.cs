using System.Net;
using OrcaFacil.Persistence.Diagnostics;

namespace OrcaFacil.Web.Middleware;

/// <summary>Stops database-dependent requests before authentication, EF, or application services are invoked.</summary>
public sealed class DatabaseReadinessMiddleware(RequestDelegate next)
{
    private static readonly string[] ProtectedPrefixes =
    [
        "/dashboard", "/clients", "/services", "/documents", "/templates", "/subscription",
        "/profile", "/notifications", "/admin"
    ];

    public async Task InvokeAsync(HttpContext context, IDatabaseConfigurationState state, IHostEnvironment environment)
    {
        if (state.IsValid || !RequiresDatabase(context.Request))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        var correlationId = WebUtility.HtmlEncode(context.TraceIdentifier);
        var message = context.Request.Path.StartsWithSegments("/Auth/Register", StringComparison.OrdinalIgnoreCase)
            ? "Não conseguimos concluir seu cadastro porque o serviço de dados ainda não está configurado corretamente. Nenhum dado foi salvo."
            : context.Request.Path.StartsWithSegments("/Auth/Login", StringComparison.OrdinalIgnoreCase)
                ? (environment.IsDevelopment()
                    ? "Banco de dados indisponível ou connection string inválida. Verifique DefaultConnection."
                    : "Serviço temporariamente indisponível. Tente novamente em instantes.")
                : state.PublicMessage;
        var hint = environment.IsDevelopment()
            ? "<p><strong>Desenvolvimento:</strong> Edite src/OrcaFacil.Web/appsettings.Local.json e reinicie a aplicação.</p>"
            : "";
        await context.Response.WriteAsync($$"""
            <!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
            <title>Serviço de dados indisponível</title></head><body><main><h1>Serviço temporariamente indisponível</h1>
            <p>{{WebUtility.HtmlEncode(message)}}</p>{{hint}}<p>Identificador: <code>{{correlationId}}</code></p>
            <p><a href="/Support">Falar com o suporte</a></p></main></body></html>
            """);
    }

    private static bool RequiresDatabase(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/Admin/Settings/Database", StringComparison.OrdinalIgnoreCase)) return false;
        if (HttpMethods.IsPost(request.Method) &&
            (request.Path.StartsWithSegments("/Auth/Register", StringComparison.OrdinalIgnoreCase) ||
             request.Path.StartsWithSegments("/Auth/Login", StringComparison.OrdinalIgnoreCase))) return true;
        return ProtectedPrefixes.Any(prefix => request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
