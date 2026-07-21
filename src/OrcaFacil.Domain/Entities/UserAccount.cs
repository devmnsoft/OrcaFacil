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
}
