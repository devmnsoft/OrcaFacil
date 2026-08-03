using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Microsoft.Extensions.Hosting;

namespace OrcaFacil.Infrastructure;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SuperAdminSeeder");
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var explicitlyAllowed = string.Equals(Environment.GetEnvironmentVariable("ORCAFACIL_ALLOW_LOCAL_SUPERADMIN_SEED"), "true", StringComparison.OrdinalIgnoreCase);
        if (environment.IsProduction())
        {
            logger.LogInformation("Seed local do SuperAdmin ignorado em produção.");
            return;
        }
        if (!explicitlyAllowed) return;

        var email = (Environment.GetEnvironmentVariable("ORCAFACIL_SUPERADMIN_EMAIL") ?? "superadmin@mnsoft.com.br").Trim().ToLowerInvariant();
        var password = Environment.GetEnvironmentVariable("ORCAFACIL_SUPERADMIN_PASSWORD") ?? "OrcaFacil@2026!Trocar";

        var users = scope.ServiceProvider.GetRequiredService<IRepository<UserAccount>>();
        if (users.Query().Any(user => user.Email == email)) return;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await users.AddAsync(new UserAccount { Name = "SuperAdmin", Email = email, PasswordHash = hasher.Hash(password), Role = UserRole.SuperAdmin, Plan = PlanType.Business, IsActive = true, MustChangePassword = true, PasswordResetReason = "LocalBootstrap" }, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("SuperAdmin criado via seed opcional.");
    }
}
