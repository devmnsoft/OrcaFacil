using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Auth;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Payments;
using OrcaFacil.Application.Notifications;
using OrcaFacil.Application.Billing;
using OrcaFacil.Infrastructure.Payments;
using OrcaFacil.Application.Profile;
using OrcaFacil.Infrastructure;
using OrcaFacil.Infrastructure.Pdf;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Queries;
using OrcaFacil.Persistence.Repositories;
using OrcaFacil.Web.Health;
using OrcaFacil.Persistence.Diagnostics;
using OrcaFacil.Persistence.Plans;
using OrcaFacil.Web.Services;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseConfigured = DatabaseConnectionOptions.TryCreate(builder.Configuration, out var databaseOptions, out var databaseConfigurationError);
if (!databaseConfigured)
{
    Log.Logger.Error("Configuração de banco inválida: {ConfigurationError}", databaseConfigurationError);
}
if (databaseOptions is not null) builder.Services.AddSingleton(databaseOptions);
builder.Services.AddDbContext<OrcaFacilDbContext>(options => options
    .UseNpgsql(defaultConnection ?? "Host=localhost;Database=postgres;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=orcafacil,public;Application Name=orcafacil.api")
    .EnableSensitiveDataLogging(false)
    .EnableDetailedErrors(builder.Environment.IsDevelopment() && builder.Configuration.GetValue("Diagnostics:EnableEfDetailedErrors", false)));
var keyPath = builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".keys");
builder.Services.AddDataProtection().SetApplicationName("OrcaFacil").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDocumentQueries, DocumentQueries>();
builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
builder.Services.AddScoped<SuperAdminDashboardQueries>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentAccountService, CurrentAccountService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<IDocumentNumberService, DocumentNumberService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PlanLimitService>();
builder.Services.AddScoped<PlanEntitlementService>();
builder.Services.AddScoped<IPlanAccessService, PlanAccessService>();
builder.Services.AddScoped<IPlanAccessDataSource, EfPlanAccessDataSource>();
builder.Services.AddScoped<TrialProService>();
builder.Services.Configure<PlanOptions>(builder.Configuration.GetSection("Plans"));
builder.Services.AddScoped<BillingStatusService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection("MercadoPago"));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
builder.Services.AddScoped<IPaymentGateway, MercadoPagoPaymentGateway>();
builder.Services.AddScoped<UserUsageService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IPdfService, QuestPdfDocumentService>();
builder.Services.AddScoped<INumberToWordsService, NumberToWordsPtBrService>();
builder.Services.AddSingleton<IDatabaseDiagnosticsService, DatabaseDiagnosticsService>();
builder.Services.AddSingleton<DatabaseDiagnosticsService>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";
    options.Events.OnValidatePrincipal = async context =>
    {
        var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var versionValue = context.Principal?.FindFirstValue("session_version");
        if (!Guid.TryParse(idValue, out var userId) || !int.TryParse(versionValue, out var sessionVersion))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<OrcaFacilDbContext>();
        var platformUser = context.Principal?.IsInRole("SuperAdministrator") == true || context.Principal?.IsInRole("SuperAdmin") == true ||
                           context.Principal?.IsInRole("PlatformSupport") == true || context.Principal?.IsInRole("PlatformFinance") == true ||
                           context.Principal?.IsInRole("PlatformAuditor") == true;
        var valid = await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive && !x.IsBlocked &&
            x.SessionVersion == sessionVersion && !x.IsDeleted && (platformUser ||
            db.AccountMembers.Any(m => m.UserId == x.Id && !m.IsDeleted && m.Status == OrcaFacil.Domain.Enums.AccountMemberStatus.Active &&
                db.BusinessAccounts.Any(a => a.Id == m.AccountId && !a.IsDeleted && a.Status == OrcaFacil.Domain.Enums.AccountStatus.Active))));
        if (!valid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin"));
    // "SuperAdmin" remains accepted while existing accounts are migrated to the
    // canonical platform role name. Authorization is enforced by the backend.
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin"));
    options.AddPolicy("PlatformSupportOrHigher", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin", "PlatformSupport"));
    options.AddPolicy("PlatformFinanceOrHigher", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin", "PlatformFinance"));
    options.AddPolicy("PlatformAuditRead", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin", "PlatformSupport", "PlatformFinance", "PlatformAuditor"));
    options.AddPolicy("PlatformPlanManagement", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin"));
    options.AddPolicy("PlatformPaymentManagement", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin", "PlatformFinance"));
    options.AddPolicy("PlanManagement", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin"));
    options.AddPolicy("PaymentManagement", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin", "PlatformFinance"));
    options.AddPolicy("SystemSettingsManagement", policy => policy.RequireRole("SuperAdministrator", "SuperAdmin"));
});
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("public", limiter =>
{
    limiter.PermitLimit = 20;
    limiter.Window = TimeSpan.FromMinutes(1);
}));
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (databaseConfigured) await SuperAdminSeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
static Task WritePublicHealth(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var status = report.Status.ToString();
    if (report.Status == HealthStatus.Unhealthy) context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    return context.Response.WriteAsync(JsonSerializer.Serialize(new { status, correlationId = context.TraceIdentifier }));
}
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live"), ResponseWriter = WritePublicHealth });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WritePublicHealth });
app.MapPost("/api/webhooks/mercadopago", async (HttpRequest request, OrcaFacil.Application.Abstractions.IPaymentGateway gateway, OrcaFacil.Persistence.OrcaFacilDbContext db, CancellationToken ct) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync(ct);
    var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
    var result = await gateway.HandleWebhookAsync(body, headers, ct);
    if (!await db.MercadoPagoWebhookEvents.AnyAsync(x => x.EventKey == result.EventKey, ct))
    {
        db.MercadoPagoWebhookEvents.Add(new OrcaFacil.Domain.Entities.MercadoPagoWebhookEvent { EventKey = result.EventKey, ExternalPaymentId = result.ExternalPaymentId, RawJson = body, Processed = true, CorrelationId = OrcaFacil.Web.HttpContextTrace.Current(request.HttpContext) });
        await db.SaveChangesAsync(ct);
    }
    return Results.Ok(new { received = true, result.EventKey });
}).AllowAnonymous();
app.MapGet("/health/version", () => new { app = "OrcaFacil", version = "1.0.0", environment = app.Environment.EnvironmentName, date = DateTime.UtcNow });
app.MapControllers();
app.MapRazorPages();
app.MapGet("/Documents/Pdf/{id:guid}", async Task<IResult> (Guid id, OrcaFacil.Application.Abstractions.ICurrentUserService currentUser, OrcaFacil.Application.Abstractions.IPdfService pdf, OrcaFacil.Persistence.OrcaFacilDbContext db, CancellationToken ct) =>
{
    var document = await db.Documents.Include(d => d.Items).SingleOrDefaultAsync(d => d.Id == id && d.UserId == currentUser.UserId && !d.IsDeleted, ct);
    if (document is null) return Results.NotFound();
    var issuer = await db.IssuerProfiles.SingleOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
    var plan = Enum.TryParse<OrcaFacil.Domain.Enums.PlanType>(currentUser.Plan, out var parsedPlan) ? parsedPlan : OrcaFacil.Domain.Enums.PlanType.Free;
    var bytes = await pdf.GenerateDocumentPdfAsync(document, issuer, plan, ct);
    return Results.File(bytes, "application/pdf", $"{document.Number}.pdf");
}).RequireAuthorization();
app.Run();
