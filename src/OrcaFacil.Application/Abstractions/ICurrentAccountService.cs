using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Abstractions;

/// <summary>
/// Resolves the authenticated user's account boundary. Account identifiers supplied by
/// requests must never be used as an authorization decision.
/// </summary>
public interface ICurrentAccountService
{
    Guid UserId { get; }
    Guid? AccountId { get; }
    Guid? AccountMemberId { get; }
    string? AccountRoleCode { get; }
    AccountStatus? AccountStatus { get; }
    bool IsPlatformUser { get; }
    bool HasAccount { get; }
    Task<bool> HasPermissionAsync(string permissionCode, CancellationToken ct = default);
    Task EnsureAccountAccessAsync(CancellationToken ct = default);
}
