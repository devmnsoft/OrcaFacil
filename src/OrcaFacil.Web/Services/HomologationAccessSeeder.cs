using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>
/// Creates one independent homologation account per active commercial plan already stored in the database.
/// Re-running does not duplicate rows, reset passwords, or change an existing subscription.
/// </summary>
public static class HomologationAccessSeeder
{
    private static readonly string[] BaselineModules = ["CORE", "CLIENTS", "DOCUMENTS", "ACCOUNT_ADMIN"];
    private static readonly string[] JourneyModules = ["WORK_ORDERS", "FINANCIAL", "SCHEDULE"];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("HomologationAccessSeeder");
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsProduction())
        {
            logger.LogWarning("Seed de homologação recusado em produção.");
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var now = DateTime.UtcNow;
        var plans = await db.Plans.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        if (plans.Count == 0)
        {
            logger.LogWarning("Nenhum plano ativo encontrado. Nenhuma conta de homologação foi criada.");
            return;
        }

        var versions = await db.PlanVersions.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == PlanVersionStatus.Published && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil > now))
            .ToListAsync(ct);
        var modules = await db.SaasModules.Where(x => x.IsActive && !x.IsDeleted).ToListAsync(ct);
        var created = new List<string>();

        foreach (var plan in plans)
        {
            if (!TryMapPlan(plan.Code, out var planType))
            {
                logger.LogWarning("Plano {PlanCode} não tem correspondência em PlanType. Conta não criada.", plan.Code);
                continue;
            }

            var version = versions.Where(x => x.PlanId == plan.Id).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (version is null)
            {
                logger.LogWarning("Plano {PlanCode} não tem versão publicada vigente. Conta não criada.", plan.Code);
                continue;
            }

            var email = $"homolog.{plan.Code.Trim().ToLowerInvariant()}@orcafacil.invalid";
            await EnsureAccountAsync(db, hasher, logger, created, email, StableAccountName(plan.Code), plan.Code, planType, version.Id, trial: false, now, modules, ct);
        }

        var professional = plans.FirstOrDefault(x => x.Code.Equals("PROFESSIONAL", StringComparison.OrdinalIgnoreCase));
        var professionalVersion = professional is null ? null : versions.Where(x => x.PlanId == professional.Id).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        if (professionalVersion is not null)
        {
            await EnsureAccountAsync(db, hasher, logger, created, "homolog.trial15@orcafacil.invalid", "Homologação teste 15 dias",
                "PROFESSIONAL", PlanType.Professional, professionalVersion.Id, trial: true, now, modules, ct);
        }
        else
        {
            logger.LogWarning("Conta de teste de 15 dias não criada: plano PROFESSIONAL sem versão publicada.");
        }

        if (created.Count == 0)
        {
            logger.LogInformation("Seed de homologação reaplicado sem criar contas nem alterar senhas ou planos.");
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), "orcafacil-homologation-credentials.txt");
        await File.WriteAllLinesAsync(path, created, ct);
        logger.LogInformation("Credenciais novas gravadas fora do repositório em {Path}. Senhas não foram registradas no log.", path);
    }

    private static async Task EnsureAccountAsync(OrcaFacilDbContext db, IPasswordHasher hasher, ILogger logger, List<string> created,
        string email, string name, string planCode, PlanType planType, Guid versionId, bool trial, DateTime now,
        List<SaasModule> modules, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted, ct))
        {
            logger.LogInformation("Conta {Email} já existe. Senha e plano preservados.", email);
            return;
        }

        var password = CreatePassword();
        var document = NextValidCpf(email);
        for (var suffix = 1; await db.BusinessAccounts.AnyAsync(x => x.DocumentNumber == document && !x.IsDeleted, ct); suffix++)
            document = NextValidCpf(email + suffix);
        var user = new UserAccount
        {
            Name = name,
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.User,
            Plan = planType,
            IsActive = true,
            MustChangePassword = true,
            PasswordResetReason = "HomologationBootstrap",
            AcceptedTermsAt = now,
            AcceptedPrivacyAt = now
        };
        var account = new BusinessAccount
        {
            DisplayName = name,
            PersonType = PersonType.Individual,
            DocumentType = BrazilianDocumentType.CPF,
            DocumentNumber = document,
            Email = email,
            Phone = "11999990000",
            City = "São Paulo",
            State = "SP",
            CurrentPlanCode = planCode
        };
        var member = new AccountMember { AccountId = account.Id, UserId = user.Id, RoleCode = "Owner" };
        member.Join();
        var trialEnds = now.AddDays(15);
        var subscription = new Subscription
        {
            AccountId = account.Id,
            UserId = user.Id,
            Plan = planType,
            SelectedPlanVersionId = versionId,
            EffectivePlanVersionId = versionId,
            Status = trial ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            TrialStatus = trial ? TrialStatus.Active : TrialStatus.NotStarted,
            TrialStartedAt = trial ? now : null,
            TrialEndsAt = trial ? trialEnds : null,
            TrialUsed = trial,
            ExpiresAt = trial ? trialEnds : null,
            Provider = trial ? "Trial" : "Homologation",
            PriceAtActivation = 0m,
            Amount = 0m,
            StartedAt = now
        };
        var billing = new BillingCustomerProfile
        {
            AccountId = account.Id, UserId = user.Id, PersonType = PersonType.Individual,
            DocumentType = BrazilianDocumentType.CPF, DocumentNumber = document, Name = name,
            Email = email, Phone = "11999990000", City = "São Paulo", State = "SP"
        };
        var issuer = new IssuerProfile { UserId = user.Id, BusinessName = name, DocumentNumber = document, Phone = "11999990000", Email = email, City = "São Paulo" };
        var allowed = new HashSet<string>(BaselineModules, StringComparer.OrdinalIgnoreCase);
        if (!planCode.Equals("FREE", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var code in JourneyModules) allowed.Add(code);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.Users.Add(user);
        db.BusinessAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        db.AccountMembers.Add(member);
        db.Subscriptions.Add(subscription);
        db.BillingCustomerProfiles.Add(billing);
        db.IssuerProfiles.Add(issuer);
        foreach (var module in modules.Where(x => allowed.Contains(x.Code)))
        {
            var moduleSubscription = new AccountModuleSubscription
            {
                AccountId = account.Id,
                ModuleId = module.Id,
                BillingPeriod = SaasBillingPeriod.Monthly,
                ContractedPrice = 0,
                StartsAt = now
            };
            if (trial) moduleSubscription.StartTrial(user.Id, trialEnds);
            else moduleSubscription.Activate(user.Id);
            db.AccountModuleSubscriptions.Add(moduleSubscription);
            db.AccountModuleEntitlements.Add(new AccountModuleEntitlement
            {
                AccountId = account.Id,
                ModuleId = module.Id,
                IsEnabled = true,
                Source = SaasModuleGrantSource.Subscription,
                GrantedByUserId = user.Id,
                Reason = trial ? "Homologação do teste de 15 dias" : "Homologação do plano cadastrado"
            });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        created.Add($"{(trial ? "Teste 15 dias" : "Responsável")}|{name}|{planCode}|{email}|{password}|/Auth/Login");
        logger.LogInformation("Conta de homologação criada para o plano {PlanCode}.", planCode);
    }

    private static string StableAccountName(string code) => code.Trim().ToUpperInvariant() switch
    {
        "FREE" => "Homologação Grátis",
        "PROFESSIONAL" => "Homologação Profissional",
        "BUSINESS" => "Homologação Negócio",
        "ENTERPRISE" => "Homologação Enterprise",
        _ => $"Homologação {code.Trim()}"
    };

    private static bool TryMapPlan(string code, out PlanType planType)
    {
        switch (code.Trim().ToUpperInvariant())
        {
            case "FREE": planType = PlanType.Free; return true;
            case "PROFESSIONAL": planType = PlanType.Professional; return true;
            case "BUSINESS": planType = PlanType.Business; return true;
            case "ENTERPRISE": planType = PlanType.Enterprise; return true;
            default: planType = default; return false;
        }
    }

    private static string CreatePassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var bytes = RandomNumberGenerator.GetBytes(18);
        var chars = new char[18];
        for (var i = 0; i < chars.Length; i++) chars[i] = alphabet[bytes[i] % alphabet.Length];
        return "Tmp-" + new string(chars);
    }

    private static string NextValidCpf(string email)
    {
        var seed = Math.Abs(BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(email)), 0));
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var digits = ((seed + attempt) % 900000000 + 100000000).ToString();
            var first = CpfDigit(digits, 9);
            var second = CpfDigit(digits + first, 10);
            var cpf = digits + first + second;
            if (BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, cpf)) return cpf;
        }

        return "39053344705";
    }

    private static int CpfDigit(string numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++) sum += (numbers[i] - '0') * (length + 1 - i);
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
