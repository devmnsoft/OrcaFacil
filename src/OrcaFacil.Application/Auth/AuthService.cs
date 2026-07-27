using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.ValueObjects;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Shared;

namespace OrcaFacil.Application.Auth;

public class AuthService
{
    private readonly IRepository<UserAccount> _users;
    private readonly IRepository<BusinessAccount> _accounts;
    private readonly IRepository<AccountMember> _members;
    private readonly IRepository<BillingCustomerProfile> _billingProfiles;
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<IssuerProfile> _issuerProfiles;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IRepository<UserAccount> users, IRepository<BusinessAccount> accounts,
        IRepository<AccountMember> members, IRepository<BillingCustomerProfile> billingProfiles,
        IRepository<Subscription> subscriptions, IRepository<IssuerProfile> issuerProfiles,
        IRepository<Notification> notificationRepository, IPasswordHasher hasher, IUnitOfWork uow,
        IAuditService audit, ILogger<AuthService> logger)
    {
        _users = users;
        _accounts = accounts;
        _members = members;
        _billingProfiles = billingProfiles;
        _subscriptions = subscriptions;
        _issuerProfiles = issuerProfiles;
        _notificationRepository = notificationRepository;
        _hasher = hasher;
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<UserSummaryDto>> RegisterAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
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

            var user = new UserAccount
            {
                Name = command.Name.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(command.Password),
                AcceptedTermsAt = DateTime.UtcNow,
                AcceptedPrivacyAt = DateTime.UtcNow,
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
            var subscription = new Subscription { AccountId = account.Id, UserId = user.Id, Plan = PlanType.Free, Status = SubscriptionStatus.Free, Provider = "None", PriceAtActivation = 0m, Amount = 0m, StartedAt = DateTime.UtcNow };
            var issuer = new IssuerProfile { UserId = user.Id, BusinessName = account.DisplayName, DocumentNumber = document, Phone = command.Phone.Trim(), Email = email, City = command.City.Trim(), Address = BuildAddress(command) };
            var notification = new Notification { AccountId = account.Id, UserId = user.Id, Title = "Conta criada", Message = "Conta criada. Vamos preparar seu espaço.", Type = NotificationType.Success, Category = NotificationCategory.Account, ActionUrl = "/Onboarding", ActionText = "Continuar" };

            await _users.AddAsync(user, ct);
            await _accounts.AddAsync(account, ct);
            await _members.AddAsync(member, ct);
            await _billingProfiles.AddAsync(billingProfile, ct);
            await _subscriptions.AddAsync(subscription, ct);
            await _issuerProfiles.AddAsync(issuer, ct);
            await _notificationRepository.AddAsync(notification, ct);
            await _audit.RegisterAsync(user.Id, "ACCOUNT_REGISTERED", nameof(BusinessAccount), account.Id.ToString(), null, new { account.Id, AccountType = command.AccountType.ToString() }, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("ACCOUNT_REGISTERED {UserId} {AccountId} {DocumentType}", user.Id, account.Id, documentType);
            return Result<UserSummaryDto>.Ok(ToSummary(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário");
            throw;
        }
    }

    public async Task<Result<UserSummaryDto>> LoginAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        try
        {
            var email = new Email(command.Email).Value;
            var user = _users.Query().SingleOrDefault(candidate => candidate.Email == email);
            if (user is null || !_hasher.Verify(command.Password, user.PasswordHash))
            {
                _logger.LogWarning("AUTH_LOGIN_FAILED {Email}", email);
                return Result<UserSummaryDto>.Fail("Credenciais inválidas.");
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

    private static UserSummaryDto ToSummary(UserAccount user) => new(user.Id, user.Name, user.Email, user.Role.ToString(), user.Plan.ToString());

    private static string? BuildAddress(RegisterUserCommand command)
    {
        var parts = new[] { command.Street, command.StreetNumber, command.Complement, command.District, command.City, command.State };
        var address = string.Join(", ", parts.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }
}
