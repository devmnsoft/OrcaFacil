using System.Text.Json;
using System.Text.RegularExpressions;

namespace OrcaFacil.Application.Marketplace;

public sealed record PackageItemManifest(string Type, string Code, string Name, JsonElement Configuration);
public sealed record PackageManifest(string Code, string Name, string Description, string Category, string TargetSegment,
    string Version, string MinimumAppVersion, IReadOnlyList<string> RequiredFeatures, IReadOnlyList<string> RequiredPlanCodes,
    IReadOnlyList<PackageItemManifest> Items, IReadOnlyList<string> Dependencies, string InstallNotes, string RollbackStrategy);
public sealed record ManifestValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class PackageManifestValidator
{
    private static readonly Regex SemanticVersion = new("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-[0-9A-Za-z.-]+)?$", RegexOptions.Compiled);
    private static readonly HashSet<string> ItemTypes = new(StringComparer.OrdinalIgnoreCase) { "CustomField", "DynamicForm", "Checklist", "Pipeline", "Workflow", "Automation", "Template", "ValidationRule", "NotificationRule", "Dashboard", "Report", "HelpArticle", "GuidedTour", "OnboardingPlaybook" };
    public static ManifestValidationResult Validate(PackageManifest manifest, IReadOnlySet<string> knownFeatures, IReadOnlySet<string> knownPlans, IReadOnlySet<string> knownPackages)
    {
        var errors = new List<string>();
        if (!Regex.IsMatch(manifest.Code ?? "", "^[a-z0-9][a-z0-9.-]{2,79}$")) errors.Add("Code inválido.");
        if (!SemanticVersion.IsMatch(manifest.Version ?? "")) errors.Add("Version deve usar versionamento semântico.");
        if (!SemanticVersion.IsMatch(manifest.MinimumAppVersion ?? "")) errors.Add("MinimumAppVersion inválida.");
        foreach (var feature in manifest.RequiredFeatures.Distinct(StringComparer.OrdinalIgnoreCase)) if (!knownFeatures.Contains(feature)) errors.Add($"Feature desconhecida: {feature}.");
        foreach (var plan in manifest.RequiredPlanCodes.Distinct(StringComparer.OrdinalIgnoreCase)) if (!knownPlans.Contains(plan)) errors.Add($"Plano desconhecido: {plan}.");
        foreach (var dependency in manifest.Dependencies.Distinct(StringComparer.OrdinalIgnoreCase)) if (dependency == manifest.Code || !knownPackages.Contains(dependency)) errors.Add($"Dependência inválida ou ausente: {dependency}.");
        if (manifest.Items.Count == 0) errors.Add("O pacote deve conter ao menos um item de configuração.");
        foreach (var item in manifest.Items) { if (!ItemTypes.Contains(item.Type)) errors.Add($"Tipo de item não permitido: {item.Type}."); if (string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name)) errors.Add("Todo item precisa de Code e Name."); if (ContainsUnsafeContent(item.Configuration.GetRawText())) errors.Add($"Conteúdo inseguro no item {item.Code}."); }
        if (manifest.Items.GroupBy(x => $"{x.Type}:{x.Code}", StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) errors.Add("O manifesto contém itens duplicados.");
        return new(errors.Count == 0, errors);
    }
    public static bool ContainsUnsafeContent(string value) => Regex.IsMatch(value, @"<\s*script|javascript\s*:|\bon\w+\s*=|\b(exec|system|process\.start)\s*\(|\b(drop|alter|truncate)\s+table\b", RegexOptions.IgnoreCase);
}

public sealed record PackagePreviewItem(string Type, string Code, string Name, string Action, bool RequiresConfirmation);
public sealed record PackagePreview(Guid PackageId, Guid VersionId, IReadOnlyList<PackagePreviewItem> Items, IReadOnlyList<string> MissingDependencies, IReadOnlyList<string> MissingFeatures, bool CanInstall)
{ public const string IncompatiblePlanMessage = "Este pacote exige recursos que não estão disponíveis no plano atual da conta."; }

public static class MarketplacePermissions
{
    public const string View="Marketplace.View", Install="Marketplace.Install", Rollback="Marketplace.Rollback", Update="Marketplace.Update", Review="Marketplace.Review", AdminView="Marketplace.AdminView", AdminManage="Marketplace.AdminManage", TemplatesView="Templates.LibraryView", TemplatesManage="Templates.LibraryManage", ConfigurationExport="Configuration.Export", ConfigurationImport="Configuration.Import", SetupWizard="SetupWizard.Use", AddonsView="Addons.View", AddonsManage="Addons.Manage", AddonsInstall="Addons.Install", AddonsRemove="Addons.Remove";
}
