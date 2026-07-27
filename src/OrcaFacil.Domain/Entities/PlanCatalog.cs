using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Plan : Entity { public string Code { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public string ShortDescription { get; set; } = string.Empty; public bool IsFree { get; set; } public bool IsActive { get; set; } = true; public bool IsPublic { get; set; } = true; public bool IsRecommended { get; set; } public int DisplayOrder { get; set; } }
public class PlanVersion : Entity { public Guid PlanId { get; set; } public int VersionNumber { get; set; } public decimal MonthlyPrice { get; set; } public decimal AnnualPrice { get; set; } public string Currency { get; set; } = "BRL"; public int TrialDays { get; set; } public int GracePeriodDays { get; set; } public DateTime ValidFrom { get; set; } public DateTime? ValidUntil { get; set; } public PlanVersionStatus Status { get; set; } = PlanVersionStatus.Draft; public DateTime? PublishedAt { get; set; } }
public class Feature : Entity { public string Code { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public PlanFeatureValueType ValueType { get; set; } public string Category { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
public class PlanFeatureValue : Entity { public Guid PlanVersionId { get; set; } public Guid FeatureId { get; set; } public bool? BooleanValue { get; set; } public int? IntegerValue { get; set; } public decimal? DecimalValue { get; set; } public string? TextValue { get; set; } public bool IsUnlimited { get; set; } }
public class Role : Entity { public string Code { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public bool IsPlatformRole { get; set; } public bool IsSystem { get; set; } }
public class Permission : Entity { public string Code { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public bool IsPlatformPermission { get; set; } }
public class RolePermission : Entity { public Guid RoleId { get; set; } public Guid PermissionId { get; set; } }
