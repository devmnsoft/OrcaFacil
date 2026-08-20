using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum TeamType { Commercial, Operational, Financial, Administrative, Support }
public enum ApprovalStatus { Pending, Approved, Rejected, Canceled, Expired }
public enum ApprovalEventType { Requested, Approved, Rejected, Canceled, Commented, Reassigned }

public sealed class BusinessUnit : Entity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BusinessUnitMember : Entity
{ public Guid AccountId { get; set; } public Guid BusinessUnitId { get; set; } public Guid UserId { get; set; } public bool IsActive { get; set; } = true; }

public sealed class Team : Entity
{ public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public string Name { get; set; } = string.Empty; public string? Description { get; set; } public TeamType Type { get; set; } public bool IsActive { get; set; } = true; }

public sealed class TeamMember : Entity
{ public Guid AccountId { get; set; } public Guid TeamId { get; set; } public Guid UserId { get; set; } public string? RoleInTeam { get; set; } public bool IsLeader { get; set; } }

public sealed class RoleProfile : Entity
{ public Guid AccountId { get; set; } public string Name { get; set; } = string.Empty; public string? Description { get; set; } public bool IsSystem { get; set; } public bool IsActive { get; set; } = true; }

public sealed class RoleProfilePermission : Entity
{ public Guid AccountId { get; set; } public Guid RoleProfileId { get; set; } public string PermissionCode { get; set; } = string.Empty; public bool IsEnabled { get; set; } }

public sealed class DiscountPolicy : Entity
{
    public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public string Name { get; set; } = string.Empty;
    public decimal MaxDiscountPercentWithoutApproval { get; set; }
    public decimal MaxDiscountAmountWithoutApproval { get; set; }
    public decimal? RequiresApprovalAboveAmount { get; set; }
    public bool RequireDifferentApprover { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class ApprovalRequest : Entity
{
    public Guid AccountId { get; set; } public Guid DocumentId { get; set; } public Guid RequestedByUserId { get; set; }
    public Guid? ApproverUserId { get; set; } public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string Reason { get; set; } = string.Empty; public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; } public bool RequireDifferentApprover { get; set; } = true;
}

public sealed class ApprovalRequestEvent : Entity
{ public Guid AccountId { get; set; } public Guid ApprovalRequestId { get; set; } public Guid ActorUserId { get; set; } public ApprovalEventType Type { get; set; } public string? Comment { get; set; } }

public sealed class WhiteLabelSetting : Entity
{ public Guid AccountId { get; set; } public string? DisplayName { get; set; } public string? LogoPath { get; set; } public string PrimaryColor { get; set; } = "#155eef"; public string SecondaryColor { get; set; } = "#344054"; public string? FooterText { get; set; } public bool RemoveOrcaFacilBrand { get; set; } }

public sealed class UnitBrandingProfile : Entity
{ public Guid AccountId { get; set; } public Guid BusinessUnitId { get; set; } public string? TradeName { get; set; } public string? LogoPath { get; set; } public string? DocumentLogoPath { get; set; } public string? PrimaryColor { get; set; } public string? SecondaryColor { get; set; } public string? FooterText { get; set; } public string? EmailText { get; set; } }

public sealed class DocumentVisibilityRule : Entity
{ public Guid AccountId { get; set; } public Guid? BusinessUnitId { get; set; } public Guid? TeamId { get; set; } public Guid? UserId { get; set; } public bool RestrictToAssignments { get; set; } }
