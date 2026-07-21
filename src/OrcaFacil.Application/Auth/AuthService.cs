using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.ValueObjects;
using OrcaFacil.Shared;

namespace OrcaFacil.Application.Auth;

public class AuthService
{
    private readonly IRepository<UserAccount> _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IRepository<UserAccount> users, IPasswordHasher hasher, IUnitOfWork uow, IAuditService audit, ILogger<AuthService> logger)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<Result<UserSummaryDto>> RegisterAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        try
        {
            var email = new Email(command.Email).Value;
            if (!command.AcceptTerms || !command.AcceptPrivacy)
            {
                return Result<UserSummaryDto>.Fail("Termos e privacidade são obrigatórios.");
            }

            if (_users.Query().Any(user => user.Email == email))
            {
                return Result<UserSummaryDto>.Fail("E-mail já cadastrado.");
            }

            var user = new UserAccount
            {
                Name = command.Name.Trim(),
                Email = email,
                PasswordHash = _hasher.Hash(command.Password),
                AcceptedTermsAt = DateTime.UtcNow,
                AcceptedPrivacyAt = DateTime.UtcNow,
            };

            await _users.AddAsync(user, ct);
            await _audit.RegisterAsync(user.Id, "USER_REGISTERED", nameof(UserAccount), user.Id.ToString(), null, new { user.Id, user.Email }, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("USER_REGISTERED {UserId}", user.Id);
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
}
