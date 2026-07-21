using Microsoft.Extensions.Logging;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Shared;

namespace OrcaFacil.Application.Profile;

public class ProfileService
{
    private readonly IRepository<IssuerProfile> _profiles;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(IRepository<IssuerProfile> profiles, IUnitOfWork uow, IAuditService audit, ILogger<ProfileService> logger)
    {
        _profiles = profiles;
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public Task<IssuerProfile?> GetAsync(GetIssuerProfileQuery query, CancellationToken ct = default)
        => Task.FromResult(_profiles.Query().SingleOrDefault(profile => profile.UserId == query.UserId));

    public async Task<Result<Guid>> SaveAsync(SaveIssuerProfileCommand command, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command.BusinessName)) return Result<Guid>.Fail("Nome/Razão Social é obrigatório.");
            var profile = _profiles.Query().SingleOrDefault(item => item.UserId == command.UserId);
            if (profile is null)
            {
                profile = new IssuerProfile { UserId = command.UserId };
                await _profiles.AddAsync(profile, ct);
            }

            profile.BusinessName = command.BusinessName.Trim();
            profile.DocumentNumber = command.DocumentNumber;
            profile.Phone = command.Phone;
            profile.Email = command.Email;
            profile.Address = command.Address;
            profile.City = command.City;
            profile.PixKey = command.PixKey;
            profile.LogoPath = command.LogoPath;
            profile.Touch();
            await _audit.RegisterAsync(command.UserId, "PROFILE_UPDATED", nameof(IssuerProfile), profile.Id.ToString(), null, profile, null, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("PROFILE_UPDATED {UserId}", command.UserId);
            return Result<Guid>.Ok(profile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar perfil do emitente para {UserId}", command.UserId);
            throw;
        }
    }
}
