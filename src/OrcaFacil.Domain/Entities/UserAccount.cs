using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class UserAccount : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public PlanType Plan { get; set; } = PlanType.Free;
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? AcceptedTermsAt { get; set; }
    public DateTime? AcceptedPrivacyAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public int SessionVersion { get; private set; } = 1;
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? PasswordExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastSuccessfulLoginAt { get; set; }
    public DateTime? LastFailedLoginAt { get; set; }
    public string? PasswordResetReason { get; set; }
    public Guid? PasswordChangedByUserId { get; set; }
    public bool LegacyUnversionedAcceptance { get; set; }

    public void RevokeSessions()
    {
        checked { SessionVersion++; }
        Touch();
    }

    public void CompleteRequiredPasswordChange(string passwordHash, DateTime changedAt, Guid? changedBy = null)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        PasswordChangedAt = changedAt;
        PasswordChangedByUserId = changedBy;
        PasswordResetReason = null;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        RevokeSessions();
    }
}
