using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrcaFacil.Infrastructure;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = context.TraceIdentifier;
            }

            _logger.LogError(ex, "Unhandled application error. CorrelationId: {CorrelationId}; Path: {Path}", correlationId, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = _environment.IsDevelopment() ? "Erro inesperado" : "Não foi possível concluir a operação",
                Detail = BuildDetail(ex),
                Instance = context.Request.Path,
            };

            problem.Extensions["correlationId"] = correlationId;
            problem.Extensions["traceId"] = context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private string BuildDetail(Exception ex)
    {
        if (HasSqlState(ex, "28P01"))
        {
            return _environment.IsDevelopment()
                ? "Falha de autenticação no PostgreSQL (28P01). Verifique usuário/senha da ConnectionString DefaultConnection."
                : "Não foi possível concluir a operação. Tente novamente em instantes ou fale com o suporte MNSOFT.";
        }

        return _environment.IsDevelopment()
            ? $"{ex.GetType().Name}: {ex.Message}"
            : "Não foi possível concluir a operação. Tente novamente em instantes ou fale com o suporte MNSOFT.";
    }

    private static bool HasSqlState(Exception ex, string sqlState)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            var value = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(value, sqlState, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
