using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class AccountMember : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = "Viewer";
    public AccountMemberStatus Status { get; private set; } = AccountMemberStatus.Invited;
    public DateTime? InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? JoinedAt { get; private set; }
    public DateTime? DisabledAt { get; private set; }
    public void Join() { Status = AccountMemberStatus.Active; JoinedAt = DateTime.UtcNow; DisabledAt = null; Touch(); }
    public void Disable() { Status = AccountMemberStatus.Disabled; DisabledAt = DateTime.UtcNow; Touch(); }
    public void Block() { Status = AccountMemberStatus.Blocked; Touch(); }
    public void Reactivate() { Status = AccountMemberStatus.Active; DisabledAt = null; Touch(); }
}
