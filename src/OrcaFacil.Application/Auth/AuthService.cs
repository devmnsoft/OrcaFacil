using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.ValueObjects;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Shared;
using System.Diagnostics;

namespace OrcaFacil.Application.Auth;

public class AuthService
{
    private readonly IRepository<UserAccount> _users;
    private readonly IRepository<BusinessAccount> _accounts;
    private readonly IRepository<AccountMember> _members;
    private readonly IRepository<BillingCustomerProfile> _billingProfiles;
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<Plan> _plans;
    private readonly IRepository<PlanVersion> _planVersions;
    private readonly IRepository<IssuerProfile> _issuerProfiles;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IRepository<UserAccount> users, IRepository<BusinessAccount> accounts,
        IRepository<AccountMember> members, IRepository<BillingCustomerProfile> billingProfiles,
        IRepository<Subscription> subscriptions, IRepository<Plan> plans, IRepository<PlanVersion> planVersions,
        IRepository<IssuerProfile> issuerProfiles,
        IRepository<Notification> notificationRepository, IPasswordHasher hasher, IUnitOfWork uow,
        IAuditService audit, ILogger<AuthService> logger)
    {
        _users = users;
        _accounts = accounts;
        _members = members;
        _billingProfiles = billingProfiles;
        _subscriptions = subscriptions;
        _plans = plans;
        _planVersions = planVersions;
        _issuerProfiles = issuerProfiles;
        _notificationRepository = notificationRepository;
        _hasher = hasher;
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<UserSummaryDto>> RegisterAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        var timer = Stopwatch.StartNew();
        var correlationId = string.IsNullOrWhiteSpace(command.CorrelationId) ? "not-provided" : command.CorrelationId;
        var stage = "REGISTER_STARTED";
        var transactionStarted = false;
        _logger.LogInformation("{Stage} CorrelationId {CorrelationId} AccountType {AccountType} Result {Result}", stage, correlationId, command.AccountType, "Started");
        try
        {
            if (string.IsNullOrWhiteSpace(command.Name)) return Result<UserSummaryDto>.Fail("Informe seu nome.");
            if (string.IsNullOrWhiteSpace(command.Email)) return Result<UserSummaryDto>.Fail("Informe um e-mail válido.");
            if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8) return Result<UserSummaryDto>.Fail("A senha precisa ter pelo menos 8 caracteres.");
            string email;
            try
            {
                email = new Email(command.Email).Value;
            }
            catch (ArgumentException)
            {
                return Result<UserSummaryDto>.Fail("Informe um e-mail válido.");
            }

            if (!command.AcceptTerms || !command.AcceptPrivacy)
            {
                return Result<UserSummaryDto>.Fail(!command.AcceptTerms ? "Aceite os termos para continuar." : "Aceite a política de privacidade para continuar.");
            }
            stage = "REGISTER_VALIDATION_COMPLETED";
            LogRegistration(stage, correlationId, command.AccountType, null, timer, "Success");

            if (_users.Query().Any(user => user.Email == email && user.IsActive))
            {
                return Result<UserSummaryDto>.Fail("Não foi possível usar esses dados. Entre com seu e-mail ou use a recuperação de acesso.");
            }

            var documentType = command.AccountType == PersonType.Company ? BrazilianDocumentType.CNPJ : BrazilianDocumentType.CPF;
            var document = BrazilianDocument.Normalize(command.DocumentNumber);
            if (document is null || !BrazilianDocument.HasBasicValidLength(documentType, document) ||
                !BrazilianDocument.HasValidCheckDigits(documentType, document))
                return Result<UserSummaryDto>.Fail("Informe um CPF ou CNPJ válido.");
            if (_accounts.Query().Any(account => account.DocumentNumber == document && !account.IsDeleted))
                return Result<UserSummaryDto>.Fail("Já existe uma conta vinculada a este CPF/CNPJ. Entre com seu e-mail ou use a recuperação de acesso.");
            stage = "REGISTER_DUPLICATE_CHECK_COMPLETED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success");

            var now = DateTime.UtcNow;
            var freePlan = _plans.Query().SingleOrDefault(x => x.Code == "FREE" && x.IsActive && !x.IsDeleted);
            var freeVersion = freePlan is null ? null : _planVersions.Query()
                .Where(x => x.PlanId == freePlan.Id && x.Status == PlanVersionStatus.Published && !x.IsDeleted &&
                            x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil > now))
                .OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (freeVersion is null)
            {
                _logger.LogCritical("ACCOUNT_REGISTRATION_BLOCKED_FREE_PLAN_CONFIGURATION_MISSING");
                return Result<UserSummaryDto>.Fail("Não foi possível preparar seu plano grátis agora. Tente novamente em instantes.");
            }
            stage = "REGISTER_FREE_PLAN_RESOLVED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success");

            var user = new UserAccount
            {
                Name = command.Name.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(command.Password),
                AcceptedTermsAt = now,
                AcceptedPrivacyAt = now,
            };

            var accountName = command.AccountType == PersonType.Company
                ? command.TradeName ?? command.LegalName ?? command.Name
                : command.ProfessionalName ?? command.Name;
            var account = new BusinessAccount
            {
                DisplayName = accountName.Trim(), LegalName = command.LegalName?.Trim(),
                TradeName = command.TradeName?.Trim(), PersonType = command.AccountType,
                DocumentType = documentType, DocumentNumber = document, Email = email,
                Phone = command.Phone.Trim(), CurrentPlanCode = "FREE"
            };
            var member = new AccountMember { AccountId = account.Id, UserId = user.Id, RoleCode = "Owner" };
            member.Join();
            var billingProfile = new BillingCustomerProfile
            {
                AccountId = account.Id, UserId = user.Id, PersonType = command.AccountType,
                DocumentType = documentType, DocumentNumber = document, Name = command.Name.Trim(),
                LegalName = command.LegalName?.Trim(), TradeName = command.TradeName?.Trim(),
                Email = email, Phone = command.Phone.Trim(), PostalCode = BrazilianDocument.Normalize(command.PostalCode),
                Street = command.Street?.Trim(), StreetNumber = command.StreetNumber?.Trim(),
                Complement = command.Complement?.Trim(), District = command.District?.Trim(),
                City = command.City.Trim(), State = command.State.Trim().ToUpperInvariant()
            };
            var subscription = new Subscription
            {
                AccountId = account.Id, UserId = user.Id, Plan = PlanType.Free,
                SelectedPlanVersionId = freeVersion.Id, EffectivePlanVersionId = freeVersion.Id,
                Status = SubscriptionStatus.Free, Provider = "None", PriceAtActivation = 0m,
                Amount = 0m, StartedAt = now
            };
            var issuer = new IssuerProfile { UserId = user.Id, BusinessName = account.DisplayName, DocumentNumber = document, Phone = command.Phone.Trim(), Email = email, City = command.City.Trim(), Address = BuildAddress(command) };
            var notification = new Notification { AccountId = account.Id, UserId = user.Id, Title = "Conta criada", Message = "Conta criada. Vamos preparar seu espaço.", Type = NotificationType.Success, Category = NotificationCategory.Account, ActionUrl = "/Onboarding", ActionText = "Continuar" };

            stage = "REGISTER_ENTITIES_PREPARED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);

            await _uow.BeginTransactionAsync(ct);
            transactionStarted = true;
            stage = "REGISTER_TRANSACTION_STARTED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);

            await _users.AddAsync(user, ct);
            await _accounts.AddAsync(account, ct);
            stage = "REGISTER_PRINCIPALS_SAVE_STARTED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Started", user.Id, account.Id);
            await _uow.SaveChangesAsync(ct);
            stage = "REGISTER_PRINCIPALS_SAVE_COMPLETED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);

            await _members.AddAsync(member, ct);
            await _billingProfiles.AddAsync(billingProfile, ct);
            await _subscriptions.AddAsync(subscription, ct);
            await _issuerProfiles.AddAsync(issuer, ct);
            await _notificationRepository.AddAsync(notification, ct);
            stage = "REGISTER_DEPENDENTS_SAVE_STARTED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Started", user.Id, account.Id);
            await _audit.RegisterAsync(user.Id, "ACCOUNT_REGISTERED", nameof(BusinessAccount), account.Id.ToString(), null, new { account.Id, AccountType = command.AccountType.ToString() }, null, ct);
            stage = "REGISTER_AUDIT_CREATED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);
            await _uow.SaveChangesAsync(ct);
            stage = "REGISTER_DEPENDENTS_SAVE_COMPLETED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);
            await _uow.CommitTransactionAsync(ct);
            transactionStarted = false;
            stage = "REGISTER_TRANSACTION_COMMITTED";
            LogRegistration(stage, correlationId, command.AccountType, documentType, timer, "Success", user.Id, account.Id);
            return Result<UserSummaryDto>.Ok(ToSummary(user));
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                try { await _uow.RollbackTransactionAsync(CancellationToken.None); }
                catch (Exception rollbackException)
                {
                    _logger.LogCritical(rollbackException, "REGISTER_ROLLBACK_FAILED CorrelationId {CorrelationId} Stage {Stage}", correlationId, stage);
                }
            }
            _logger.LogError("REGISTER_FAILED CorrelationId {CorrelationId} Stage {Stage} AccountType {AccountType} ExceptionType {ExceptionType} DurationMs {DurationMs} Result {Result}", correlationId, stage, command.AccountType, ex.GetType().Name, timer.ElapsedMilliseconds, "Failed");
            throw;
        }
    }

    private void LogRegistration(string stage, string correlationId, PersonType accountType,
        BrazilianDocumentType? documentType, Stopwatch timer, string result, Guid? userId = null, Guid? accountId = null) =>
        _logger.LogInformation("{Stage} CorrelationId {CorrelationId} AccountType {AccountType} DocumentType {DocumentType} UserId {UserId} AccountId {AccountId} DurationMs {DurationMs} Result {Result}",
            stage, correlationId, accountType, documentType, userId, accountId, timer.ElapsedMilliseconds, result);

    public async Task<Result<UserSummaryDto>> LoginAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        try
        {
            var email = new Email(command.Email).Value;
            var user = _users.Query().SingleOrDefault(candidate => candidate.Email == email);
            if (user is null || !_hasher.Verify(command.Password, user.PasswordHash))
            {
                _logger.LogWarning("AUTH_LOGIN_FAILED CorrelationId {CorrelationId}", command.CorrelationId ?? "not-provided");
                return Result<UserSummaryDto>.Fail("E-mail ou senha inválidos.");
            }

            if (user.IsBlocked)
            {
                return Result<UserSummaryDto>.Fail("Usuário bloqueado.");
            }

            if (!user.IsActive)
            {
                return Result<UserSummaryDto>.Fail("Usuário inativo.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _audit.RegisterAsync(user.Id, "USER_LOGIN", nameof(UserAccount), user.Id.ToString(), null, new { user.LastLoginAt }, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("USER_LOGIN_SUCCESS {UserId}", user.Id);
            return Result<UserSummaryDto>.Ok(ToSummary(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar usuário");
            throw;
        }
    }

    private static UserSummaryDto ToSummary(UserAccount user) => new(user.Id, user.Name, user.Email, user.Role.ToString(), user.Plan.ToString(), user.SessionVersion);

    private static string? BuildAddress(RegisterUserCommand command)
    {
        var parts = new[] { command.Street, command.StreetNumber, command.Complement, command.District, command.City, command.State };
        var address = string.Join(", ", parts.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }
}
