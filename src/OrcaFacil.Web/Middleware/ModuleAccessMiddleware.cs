using System.Security.Claims;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Saas.Modules;

namespace OrcaFacil.Web.Middleware;

public sealed class ModuleAccessMiddleware(RequestDelegate next, ILogger<ModuleAccessMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, SaasModuleRegistryService registry,
        IModuleAccessService access, IModuleUsageTracker usage, ICurrentAccountService current)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var definition = registry.FindByPath(path);
        if (definition is null || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var isSuperAdmin = context.User.IsInRole("SuperAdministrator") || context.User.IsInRole("SuperAdmin");
        var requiredPermission = path.Equals("/Documents/New", StringComparison.OrdinalIgnoreCase)
            ? "Documents.Create" : definition.RequiredPermissionCode;
        var hasPermission = isSuperAdmin;
        if (definition.Code == "ACCOUNT_ADMIN" && current.AccountRoleCode is "Owner" or "Administrator") hasPermission = true;
        if (!hasPermission)
        {
            try { hasPermission = string.IsNullOrWhiteSpace(requiredPermission) || await current.HasPermissionAsync(requiredPermission, context.RequestAborted); }
            catch (UnauthorizedAccessException) { hasPermission = false; }
        }
        var decision = await access.CheckAsync(current.AccountId, definition.Code, null, hasPermission, isSuperAdmin, context.RequestAborted);
        if (!decision.Allowed)
        {
            logger.LogWarning("MODULE_ACCESS_DENIED ModuleCode {ModuleCode} UserId {UserId} AccountId {AccountId} Path {Path}", definition.Code, context.User.FindFirstValue("user_id"), current.AccountId, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(decision.Reason ?? "Acesso não permitido.");
            return;
        }

        await next(context);
        if (current.AccountId is Guid accountId && context.Response.StatusCode < 500)
        {
            Guid? userId = Guid.TryParse(context.User.FindFirstValue("user_id"), out var parsed) ? parsed : null;
            await usage.TrackAsync(accountId, userId, definition.Code, "route.accessed", null, path,
                context.TraceIdentifier, CancellationToken.None);
        }
    }
}
