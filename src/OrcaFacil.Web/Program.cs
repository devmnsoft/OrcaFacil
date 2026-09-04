using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using OrcaFacil.Application;
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
using OrcaFacil.Persistence.Marketplace;
using OrcaFacil.Web.Services;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using OrcaFacil.Web.Configuration;
using OrcaFacil.Web.Middleware;
using OrcaFacil.Web.Email;
using OrcaFacil.Web.Security;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Persistence.Services;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Application.Clients;
using OrcaFacil.Application.Pricing;
using OrcaFacil.Application.Integrations;
using OrcaFacil.Application.Files;
using OrcaFacil.Infrastructure.Files;
using OrcaFacil.Application.Privacy;
using OrcaFacil.Application.Retention;
using OrcaFacil.Application.Security;
using OrcaFacil.Application.Jobs;
using OrcaFacil.Web.Api;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using OrcaFacil.Application.Localization;
using OrcaFacil.Application.GoLive;
using OrcaFacil.Persistence.Services.GoLive;

var builder = WebApplication.CreateBuilder(args);
var repositoryRoot = Directory.GetParent(builder.Environment.ContentRootPath)?.Parent?.FullName
    ?? builder.Environment.ContentRootPath;
builder.Services.AddApplication(repositoryRoot);
builder.Services.AddPersistence();
builder.AddOrcaFacilLocalConfiguration();
DatabaseConnectionStringResolver.ApplyOperationalAlias(builder.Configuration);
// Operational aliases keep Windows service/IIS configuration concise while the
// regular ASP.NET double-underscore variables remain supported.
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORCAFACIL_DATAPROTECTION_PATH")))
    builder.Configuration["DataProtection:KeysPath"] = Environment.GetEnvironmentVariable("ORCAFACIL_DATAPROTECTION_PATH");
if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORCAFACIL_MAINTENANCE_MODE")))
    builder.Configuration["MaintenanceMode:Enabled"] = Environment.GetEnvironmentVariable("ORCAFACIL_MAINTENANCE_MODE");
if (builder.Configuration.GetValue("Diagnostics:EnableEfCommandLogging", false))
    builder.Configuration["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Information";

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());

var databaseConfigured = DatabaseConnectionOptions.TryCreate(builder.Configuration, out var databaseOptions, out var databaseConfigurationError);
var localSettingsPath = Path.Combine(builder.Environment.ContentRootPath, LocalConfigurationExtensions.FileName);
var databaseConfigurationState = DatabaseConfigurationState.Create(builder.Configuration, localSettingsPath);
if (!databaseConfigured)
{
    Log.Logger.Error("Configuração de banco inválida: {ConfigurationError}", databaseConfigurationError);
}
builder.Services.AddSingleton<IDatabaseConfigurationState>(databaseConfigurationState);
if (databaseOptions is not null && databaseConfigured) builder.Services.AddSingleton(databaseOptions);
builder.Services.AddDbContext<OrcaFacilDbContext>(options => options
    .UseNpgsql(DatabaseConnectionStringResolver.ResolveRequired(builder.Configuration))
    .EnableSensitiveDataLogging(false)
    .EnableDetailedErrors(builder.Environment.IsDevelopment() && builder.Configuration.GetValue("Diagnostics:EnableEfDetailedErrors", false)));
var configuredKeyPath = builder.Configuration["DataProtection:KeysPath"];
var keyPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredKeyPath)
    ? Path.Combine(builder.Environment.ContentRootPath, ".keys")
    : configuredKeyPath);
Directory.CreateDirectory(keyPath);
builder.Services.AddSingleton(new DataProtectionOperationalState(keyPath, true));
builder.Services.AddDataProtection()
    .SetApplicationName(builder.Configuration["DataProtection:ApplicationName"] ?? "OrcaFacil")
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDocumentQueries, DocumentQueries>();
builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
builder.Services.AddScoped<SuperAdminDashboardQueries>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICurrentAccountService, CurrentAccountService>();
builder.Services.AddScoped<IAccountSelectionService, AccountSelectionService>();
builder.Services.AddScoped<IAccountSwitcherService, AccountSwitcherService>();
builder.Services.AddScoped<IClientShellViewModelFactory, ClientShellViewModelFactory>();
builder.Services.AddScoped<INextBestActionService, NextBestActionService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<OrcaFacil.Application.Scoring.IQuoteScoreService, OrcaFacil.Application.Scoring.QuoteScoreService>();
builder.Services.AddScoped<OrcaFacil.Application.Scoring.IClientScoreService, OrcaFacil.Application.Scoring.ClientScoreService>();
builder.Services.AddScoped<ICommercialAutomationService, CommercialAutomationService>();
builder.Services.AddScoped<IDashboardExperienceService, DashboardExperienceService>();
builder.Services.AddSingleton<IContextualHelpService, ContextualHelpService>();
builder.Services.AddScoped<GoLiveChecklistService>();
builder.Services.AddScoped<GoLivePersistenceService>();
builder.Services.AddSingleton<TrainingGuideService>();
builder.Services.AddScoped<TrainingProgressService>();
builder.Services.AddSingleton<RouteErrorFingerprintService>();
builder.Services.AddSingleton<ProductionReadinessService>();
builder.Services.AddScoped<IPlanExperienceService, PlanExperienceService>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<IInternalAssistantService, InternalAssistantService>();
builder.Services.AddSingleton<INavigationMapService, NavigationMapService>();
builder.Services.AddScoped<IIntelligenceReportService, IntelligenceReportService>();
builder.Services.AddScoped<AnalyticsV21Service>();
builder.Services.AddScoped<IOperationalAlertService, OperationalAlertService>();
builder.Services.AddScoped<IAdminShellViewModelFactory, AdminShellViewModelFactory>();
builder.Services.AddScoped<IUserSignInService, CookieUserSignInService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<DataSubjectRequestService>();
builder.Services.AddScoped<DataExportService>();
builder.Services.AddScoped<AnonymizationService>();
builder.Services.AddScoped<RetentionPolicyService>();
builder.Services.AddScoped<SensitiveDataAccessService>();
builder.Services.AddScoped<SessionSecurityService>();
builder.Services.AddScoped<IJobLockService, JobLockService>();
builder.Services.AddScoped<IProcessingOutboxService, ProcessingOutboxService>();
builder.Services.AddSingleton<QuotaService>();
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<OrcaFacil.Application.Approvals.ApprovalWorkflowService>();
builder.Services.AddScoped<BudgetWizardService>();
builder.Services.AddScoped<BudgetDraftService>();
builder.Services.AddScoped<IDocumentStatusTransitionService, DocumentStatusTransitionService>();
builder.Services.AddSingleton<IPublicDocumentTokenService, PublicDocumentTokenService>();
builder.Services.AddSingleton<IDocumentSnapshotSerializer, DocumentSnapshotSerializer>();
builder.Services.AddScoped<IWorkOrderStatusTransitionService, WorkOrderStatusTransitionService>();
var technicalFingerprintPepper = TechnicalFingerprintPepperResolver.Resolve(
    builder.Configuration,
    builder.Environment.EnvironmentName);
builder.Services.AddSingleton<ITechnicalFingerprintService>(
    _ => new OrcaFacil.Persistence.Services.TechnicalFingerprintService(technicalFingerprintPepper));
builder.Services.AddScoped<CommercialJourneyService>();
builder.Services.AddScoped<ICommercialJourneyService>(sp => sp.GetRequiredService<CommercialJourneyService>());
builder.Services.AddScoped<ICommercialWorkspaceQueryService, CommercialWorkspaceQueryService>();
builder.Services.AddScoped<IPublicDocumentAccessService>(sp => sp.GetRequiredService<CommercialJourneyService>());
builder.Services.AddScoped<IManualPaymentRegistrationService>(sp => sp.GetRequiredService<CommercialJourneyService>());
builder.Services.AddScoped<IReceiptApplicationService, ReceiptApplicationService>();
builder.Services.AddScoped<IReceiptQueryService, ReceiptQueryService>();
builder.Services.AddScoped<IClientWorkspaceService, ClientWorkspaceService>();
builder.Services.AddScoped<OrcaFacil.Application.Onboarding.IOnboardingApplicationService, OnboardingApplicationService>();
builder.Services.AddScoped<IQuoteWorkspaceService, QuoteWorkspaceService>();
builder.Services.AddScoped<IGuidedBudgetStartService, GuidedBudgetStartService>();
builder.Services.AddSingleton<IPricingEngineService, PricingEngineService>();
builder.Services.AddScoped<OrcaFacil.Application.Services.IServiceCatalogApplicationService, ServiceCatalogApplicationService>();
builder.Services.AddSingleton<OrcaFacil.Application.Services.IServiceUnitCatalog, OrcaFacil.Application.Services.ServiceUnitCatalog>();
builder.Services.AddScoped<IDocumentNumberService, DocumentNumberService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<SupportDeskService>();
builder.Services.AddScoped<AssetOperationsService>();
builder.Services.AddScoped<OmnichannelService>();
builder.Services.AddScoped<WebChatSessionService>();
builder.Services.AddScoped<InboundEmailService>();
builder.Services.AddScoped<OmnichannelWhatsAppService>();
builder.Services.AddScoped<OmnichannelOptOutService>();
builder.Services.AddScoped<PlanLimitService>();
builder.Services.AddScoped<PlanEntitlementService>();
builder.Services.AddScoped<PackagePreviewService>();
builder.Services.AddScoped<PackageInstallationService>();
builder.Services.AddScoped<PackageRollbackService>();
builder.Services.AddScoped<IPlanAccessService, PlanAccessService>();
builder.Services.AddSingleton<IPlanFeatureReadinessService, PlanFeatureReadinessService>();
builder.Services.AddScoped<IPlanAccessDataSource, EfPlanAccessDataSource>();
builder.Services.AddScoped<IPlanCatalogService, EfPlanCatalogService>();
builder.Services.AddScoped<TrialProService>();
builder.Services.Configure<PlanOptions>(builder.Configuration.GetSection("Plans"));
builder.Services.AddScoped<BillingStatusService>();
builder.Services.AddScoped<BillingPaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<FriendlyMessageService>();
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection("MercadoPago"));
builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection("Billing"));
builder.Services.AddScoped<IPaymentGateway, MercadoPagoPaymentGateway>();
builder.Services.AddScoped<ISubscriptionCheckoutService, SubscriptionCheckoutService>();
builder.Services.AddScoped<UserUsageService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection("EmailOutbox"));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection("PasswordReset"));
builder.Services.Configure<ApplicationUrlOptions>(builder.Configuration.GetSection("Application"));
builder.Services.Configure<SecuritySecretOptions>(builder.Configuration.GetSection("Security"));
builder.Services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<PasswordResetOptions>, PasswordResetOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<SecuritySecretOptions>, SecuritySecretOptionsValidator>();
builder.Services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
builder.Services.AddScoped<IPasswordRecoveryService, PasswordRecoveryService>();
builder.Services.AddScoped<IEmailSender, GmailSmtpEmailSender>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<ProductionConfigurationValidator>();
builder.Services.AddScoped<IPdfService, QuestPdfDocumentService>();
builder.Services.AddSingleton<IFileStorageService>(_ => new LocalFileStorageService(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "private-files")));
builder.Services.AddScoped<INumberToWordsService, NumberToWordsPtBrService>();
builder.Services.AddSingleton<IDatabaseDiagnosticsService, DatabaseDiagnosticsService>();
builder.Services.AddSingleton<DatabaseDiagnosticsService>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<LocalSettingsHealthCheck>("local-settings", tags: ["ready"])
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Error/403";
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
}).AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.Scheme, _ => { });
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
    foreach (var permission in OrcaFacil.Application.Security.PermissionCodes.All)
        options.AddPolicy($"Permission:{permission}", policy => policy.AddRequirements(new OrcaFacil.Web.Security.PermissionRequirement(permission)));
});
builder.Services.AddScoped<OrcaFacil.Application.Abstractions.IPermissionService, OrcaFacil.Web.Security.PermissionService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, OrcaFacil.Web.Security.PermissionAuthorizationHandler>();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("public", limiter =>
{
    limiter.PermitLimit = 20;
    limiter.Window = TimeSpan.FromMinutes(1);
}));
builder.Services.Configure<RateLimiterOptions>(options => options.AddPolicy("api", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue("api_key_id") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
builder.Services.AddControllers();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = SupportedLocales.All.Keys.Select(CultureInfo.GetCultureInfo).ToArray();
    options.DefaultRequestCulture = new RequestCulture(SupportedLocales.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.FallBackToParentCultures = false;
    options.FallBackToParentUICultures = false;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Diagnostico", "/Admin/SystemHealth");
    options.Conventions.AddPageRoute("/Subscription/Index", "/MeuPlano");
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsDevelopment() && !databaseConfigurationState.IsValid)
    throw new InvalidOperationException("DefaultConnection inválida. A aplicação não pode iniciar em Production sem banco configurado.");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (databaseConfigured) await SuperAdminSeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<CriticalRouteMonitorMiddleware>();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
        return Task.CompletedTask;
    });
    await next();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseMiddleware<DatabaseReadinessMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.MapGet("/favicon.ico", () => Results.Redirect("/favicon.svg", permanent: true));
app.MapGet("/diagnostico", () => Results.Redirect("/SystemHealth/Database", permanent: true));
app.UseRouting();
app.UseRequestLocalization();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<ApiRequestLoggingMiddleware>();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseAuthorization();
app.MapPost("/locale", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var culture = form["culture"].ToString();
    var returnUrl = form["returnUrl"].ToString();
    var normalized = SupportedLocales.Normalize(culture);
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), HttpOnly = true, IsEssential = true,
            SameSite = SameSiteMode.Lax, Secure = context.Request.IsHttps });
    var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) && returnUrl.StartsWith('/')
        ? returnUrl : "/";
    return Results.LocalRedirect(safeReturnUrl);
}).DisableAntiforgery();
static Task WritePublicHealth(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var status = report.Status.ToString();
    if (report.Status == HealthStatus.Unhealthy) context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    return context.Response.WriteAsync(JsonSerializer.Serialize(new { status, correlationId = context.TraceIdentifier }));
}
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains("live"), ResponseWriter = WritePublicHealth });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WritePublicHealth });
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = WritePublicHealth });
app.MapPost("/api/webhooks/mercadopago", async (HttpRequest request, OrcaFacil.Application.Abstractions.IPaymentGateway gateway, OrcaFacil.Persistence.OrcaFacilDbContext db, CancellationToken ct) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync(ct);
    var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
    var result = await gateway.HandleWebhookAsync(body, headers, ct);
    if (!result.Processed)
        return Results.Unauthorized();
    if (!await db.MercadoPagoWebhookEvents.AnyAsync(x => x.EventKey == result.EventKey, ct))
    {
        db.MercadoPagoWebhookEvents.Add(new OrcaFacil.Domain.Entities.MercadoPagoWebhookEvent { EventKey = result.EventKey, ExternalPaymentId = result.ExternalPaymentId, RawJson = body, Processed = true, CorrelationId = OrcaFacil.Web.HttpContextTrace.Current(request.HttpContext) });
        await db.SaveChangesAsync(ct);
    }
    return Results.Ok(new { received = true, result.EventKey });
}).AllowAnonymous();
app.MapGet("/health/version", () => new { app = "OrcaFacil", version = "1.0.0", environment = app.Environment.EnvironmentName, date = DateTime.UtcNow });
app.MapGet("/Internal/Search", async Task<IResult> (string? q, int? limit, IGlobalSearchService search, ICurrentAccountService account, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        return Results.BadRequest(new { message = "Digite ao menos dois caracteres." });
    if (!await account.HasPermissionAsync(PermissionCodes.SearchGlobal, ct)) return Results.Forbid();
    return Results.Ok(new { results = await search.SearchAsync(q, limit ?? 12, ct) });
}).RequireAuthorization();
app.MapGet("/Internal/Services/Search", async Task<IResult> (string? q, Guid? categoryId, bool? favorite, bool? recent, bool? mostUsed, int? limit, ICurrentAccountService account, OrcaFacilDbContext db, CancellationToken ct) =>
{
    if (account.AccountId is not Guid accountId) return Results.Forbid();
    var query = db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted);
    if (!string.IsNullOrWhiteSpace(q)) { var term = $"%{q.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Name, term) || (x.Description != null && EF.Functions.ILike(x.Description, term))); }
    if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId);
    if (favorite == true) query = query.Where(x => x.IsFavorite);
    query = mostUsed == true ? query.OrderByDescending(x => x.UseCount) : recent == true ? query.OrderByDescending(x => x.LastUsedAt) : query.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name);
    var take = Math.Clamp(limit ?? 12, 1, 50);
    if (account.AccountRoleCode is "Owner" or "Administrator")
    {
        var protectedResults = await query.Take(take).Select(x => new { x.Id, x.Name, x.Description, x.CategoryId, unit = x.UnitCode, price = x.StandardPrice, durationMinutes = x.SuggestedDurationMinutes, estimatedCost = x.EstimatedCost, margin = x.StandardPrice - x.EstimatedCost }).ToListAsync(ct);
        return Results.Ok(new { results = protectedResults });
    }
    var results = await query.Take(take).Select(x => new { x.Id, x.Name, x.Description, x.CategoryId, unit = x.UnitCode, price = x.StandardPrice, durationMinutes = x.SuggestedDurationMinutes }).ToListAsync(ct);
    return Results.Ok(new { results });
}).RequireAuthorization();
app.MapGet("/Internal/Accounts", async Task<IResult> (HttpContext context, IAccountSwitcherService switcher, CancellationToken ct) =>
{
    if (!Guid.TryParse(context.User.FindFirst("user_id")?.Value, out var userId)) return Results.Unauthorized();
    Guid? current = Guid.TryParse(context.User.FindFirst("account_id")?.Value, out var accountId) ? accountId : null;
    return Results.Ok(new { accounts = await switcher.GetAuthorizedAsync(userId, current, ct) });
}).RequireAuthorization();
app.MapPost("/Internal/Accounts/Switch", async Task<IResult> (AccountSwitchRequest request, HttpContext context,
    Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, IAccountSwitcherService switcher, CancellationToken ct) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var result = await switcher.SwitchAsync(context, request.AccountId, ct);
    return result.Succeeded ? Results.Ok(new { redirectUrl = "/Dashboard" }) : Results.BadRequest(new { message = result.Error });
}).RequireAuthorization();
app.MapGet("/Internal/Help/{code}", async Task<IResult> (string code, IContextualHelpService help, CancellationToken ct) =>
{
    var content = await help.GetAsync(code, ct);
    return content is null ? Results.NotFound(new { message = "Ajuda não encontrada para esta página." }) : Results.Ok(content);
}).RequireAuthorization();
app.MapControllers();
app.MapPublicApiV1();
app.MapGet("/robots.txt", (IConfiguration configuration) =>
{
    var baseUrl = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
    var sitemap = string.IsNullOrWhiteSpace(baseUrl) ? "/sitemap.xml" : $"{baseUrl}/sitemap.xml";
    return Results.Text($"User-agent: *\nAllow: /\nDisallow: /Admin\nDisallow: /Internal\nDisallow: /Portal\nDisallow: /PublicQuotes\nSitemap: {sitemap}\n", "text/plain; charset=utf-8");
});
app.MapGet("/sitemap.xml", (HttpContext context, IConfiguration configuration) =>
{
    var root = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
    if (string.IsNullOrWhiteSpace(root)) root = $"{context.Request.Scheme}://{context.Request.Host}";
    string[] routes = ["/", "/Recursos", "/Segmentos", "/Precos", "/Contato", "/Demo", "/Comecar", "/Seguranca", "/Integracoes", "/Implantacao", "/Sobre", "/Blog", "/Recursos/Materiais", "/Trust", "/Status", "/Termos", "/Privacidade", "/Cookies", "/LGPD"];
    var urls = string.Join(string.Empty, routes.Select(route => $"<url><loc>{System.Net.WebUtility.HtmlEncode(root + route)}</loc></url>"));
    return Results.Text($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">{urls}</urlset>", "application/xml; charset=utf-8");
});
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

public partial class Program;
public sealed record AccountSwitchRequest(Guid AccountId);
