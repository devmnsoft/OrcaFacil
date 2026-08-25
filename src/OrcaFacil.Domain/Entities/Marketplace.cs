using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum PackageInstallationStatus { Pending, Installing, Installed, PartiallyInstalled, Failed, Canceled, RolledBack, UpdateAvailable }

public sealed class MarketplacePackage : Entity
{
    public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = "";
    public string Category { get; set; } = ""; public string TargetSegment { get; set; } = ""; public string Author { get; set; } = "OrçaFácil";
    public bool IsActive { get; set; } public bool IsPublished { get; set; } public Guid? CurrentVersionId { get; set; }
}
public sealed class MarketplacePackageVersion : Entity
{
    public Guid PackageId { get; set; } public string Version { get; set; } = ""; public string MinimumAppVersion { get; set; } = "3.0.0";
    public string RequiredFeaturesJson { get; set; } = "[]"; public string RequiredPlanCodesJson { get; set; } = "[]";
    public string ItemsJson { get; set; } = "[]"; public string DependenciesJson { get; set; } = "[]"; public string? InstallNotes { get; set; }
    public string RollbackStrategy { get; set; } = "DeactivateInstalledConfiguration"; public string? ChangeLog { get; set; }
    public bool IsPublished { get; set; } public DateTime? PublishedAt { get; set; }
}
public sealed class MarketplacePackageInstallation : Entity
{
    public Guid AccountId { get; set; } public Guid PackageId { get; set; } public Guid PackageVersionId { get; set; }
    public Guid InstalledByUserId { get; set; } public PackageInstallationStatus Status { get; set; } = PackageInstallationStatus.Pending;
    public DateTime? StartedAt { get; set; } public DateTime? CompletedAt { get; set; } public string? FailureSummary { get; set; }
}
public sealed class MarketplacePackageInstallationItem : Entity
{
    public Guid AccountId { get; set; } public Guid InstallationId { get; set; } public string ItemType { get; set; } = "";
    public string OriginKey { get; set; } = ""; public Guid? CreatedEntityId { get; set; } public string Status { get; set; } = "Pending";
    public bool WasCreated { get; set; } public bool IsDeactivated { get; set; } public string? ErrorSummary { get; set; }
}
public sealed class MarketplacePackageInstallationEvent : Entity { public Guid AccountId { get; set; } public Guid InstallationId { get; set; } public Guid ActorUserId { get; set; } public string EventType { get; set; } = ""; public string DetailsJson { get; set; } = "{}"; }
public sealed class MarketplacePackageReview : Entity { public Guid AccountId { get; set; } public Guid PackageId { get; set; } public int Rating { get; set; } public string? Comment { get; set; } public Guid CreatedByUserId { get; set; } }
public sealed class AddonCatalogItem : Entity { public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Description { get; set; } = ""; public string FeatureCodesJson { get; set; } = "[]"; public string RequiredPlanCodesJson { get; set; } = "[]"; public bool IsActive { get; set; } }
public sealed class AddonInstallation : Entity { public Guid AccountId { get; set; } public Guid AddonId { get; set; } public Guid InstalledByUserId { get; set; } public bool IsActive { get; set; } public DateTime InstalledAt { get; set; } public DateTime? RemovedAt { get; set; } }
public sealed class AddonEntitlement : Entity { public Guid AccountId { get; set; } public Guid AddonInstallationId { get; set; } public string FeatureCode { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class TemplateLibraryItem : Entity { public Guid? AccountId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string Type { get; set; } = ""; public string TargetSegment { get; set; } = ""; public Guid? OriginPackageId { get; set; } public bool IsActive { get; set; } public Guid? CurrentVersionId { get; set; } }
public sealed class TemplateLibraryVersion : Entity { public Guid TemplateId { get; set; } public int VersionNumber { get; set; } public string ContentJson { get; set; } = "{}"; public string PreviewText { get; set; } = ""; public bool IsPublished { get; set; } }
public sealed class SetupWizardProgress : Entity { public Guid AccountId { get; set; } public Guid UserId { get; set; } public string ProfileJson { get; set; } = "{}"; public int CurrentStep { get; set; } public bool IsCompleted { get; set; } }
