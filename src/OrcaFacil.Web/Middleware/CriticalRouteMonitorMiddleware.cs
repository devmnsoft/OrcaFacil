using System.Diagnostics;
using System.Security.Claims;
using OrcaFacil.Application.GoLive;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Middleware;

public sealed class CriticalRouteMonitorMiddleware(RequestDelegate next, ILogger<CriticalRouteMonitorMiddleware> logger)
{
    private static readonly HashSet<string> Routes = new(StringComparer.OrdinalIgnoreCase) { "/", "/Auth/Login", "/Onboarding", "/Dashboard", "/Clients", "/Clients/Create", "/ServiceCatalog", "/Services", "/Documents", "/Documents/CreateBudget", "/CommercialRoutine", "/SystemHealth", "/Admin/QualityGate", "/Support", "/Training", "/Portal", "/PartnerPortal" };
    public async Task InvokeAsync(HttpContext context, OrcaFacilDbContext db, RouteErrorFingerprintService fingerprints)
    {
        var route = context.Request.Path.Value?.TrimEnd('/') is { Length: > 0 } value ? value : "/";
        if (!Routes.Contains(route)) { await next(context); return; }
        var timer = Stopwatch.StartNew(); Exception? failure = null;
        try { await next(context); }
        catch (Exception error) { failure=error; throw; }
        finally
        {
            timer.Stop();
            try
            {
                Guid? accountId=Guid.TryParse(context.User.FindFirst("account_id")?.Value,out var account)?account:null;
                Guid? userId=Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier),out var user)?user:null;
                db.CriticalRouteEvents.Add(new CriticalRouteEvent { AccountId=accountId, UserId=userId, Route=route, StatusCode=failure is null?context.Response.StatusCode:500, DurationMilliseconds=timer.ElapsedMilliseconds, CorrelationId=context.TraceIdentifier, ErrorFingerprint=failure is null?null:fingerprints.Create(failure.GetType().Name,route), SanitizedError=failure is null?null:fingerprints.Sanitize(failure.Message) });
                await db.SaveChangesAsync(context.RequestAborted);
            }
            catch (Exception monitorError) { logger.LogWarning(monitorError,"Falha não bloqueante ao registrar saúde da rota {Route}",route); }
        }
    }
}
