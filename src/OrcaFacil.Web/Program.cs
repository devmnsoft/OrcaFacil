using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Auth;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.Profile;
using OrcaFacil.Infrastructure;
using OrcaFacil.Infrastructure.Pdf;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Queries;
using OrcaFacil.Persistence.Repositories;
using OrcaFacil.Web.Health;
using OrcaFacil.Web.Diagnostics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext().WriteTo.Console());

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    Log.Logger.Error("ConnectionStrings:DefaultConnection ausente. Configure appsettings, user-secrets ou a variável ConnectionStrings__DefaultConnection.");
}
builder.Services.AddDbContext<OrcaFacilDbContext>(options => options.UseNpgsql(defaultConnection));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDocumentQueries, DocumentQueries>();
builder.Services.AddScoped<IDashboardQueries, DashboardQueries>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ILoggerService, LoggerService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<IDocumentNumberService, DocumentNumberService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PlanLimitService>();
builder.Services.AddScoped<UserUsageService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IPdfService, QuestPdfDocumentService>();
builder.Services.AddScoped<INumberToWordsService, NumberToWordsPtBrService>();
builder.Services.AddSingleton<DatabaseDiagnosticsService>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/Login";
});
builder.Services.AddAuthorization(options => options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin")));
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

await SuperAdminSeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/health/db", async (DatabaseDiagnosticsService diagnostics, CancellationToken ct) =>
{
    var result = await diagnostics.CheckAsync(ct);
    var healthy = result.Connected && result.SchemaFound && result.RequiredTables.Values.All(found => found);
    return healthy ? Results.Ok(result) : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
});
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
