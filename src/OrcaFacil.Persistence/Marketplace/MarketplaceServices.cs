using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Marketplace;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Marketplace;

public sealed class PackagePreviewService(OrcaFacilDbContext db)
{
    public async Task<PackagePreview> PreviewAsync(Guid accountId, Guid packageId, IReadOnlySet<string> entitlements, string planCode, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId é obrigatório.", nameof(accountId));
        var package = await db.MarketplacePackages.AsNoTracking().SingleAsync(x => x.Id == packageId && x.IsActive && x.IsPublished && !x.IsDeleted, ct);
        var version = await db.MarketplacePackageVersions.AsNoTracking().SingleAsync(x => x.Id == package.CurrentVersionId && x.IsPublished && !x.IsDeleted, ct);
        var requiredFeatures = JsonSerializer.Deserialize<string[]>(version.RequiredFeaturesJson) ?? [];
        var requiredPlans = JsonSerializer.Deserialize<string[]>(version.RequiredPlanCodesJson) ?? [];
        var dependencies = JsonSerializer.Deserialize<string[]>(version.DependenciesJson) ?? [];
        var installedDependencies = await (from installation in db.MarketplacePackageInstallations.AsNoTracking() join p in db.MarketplacePackages.AsNoTracking() on installation.PackageId equals p.Id where installation.AccountId == accountId && installation.Status == PackageInstallationStatus.Installed && !installation.IsDeleted select p.Code).ToListAsync(ct);
        var missingDependencies = dependencies.Except(installedDependencies, StringComparer.OrdinalIgnoreCase).ToArray();
        var missingFeatures = requiredFeatures.Where(x => !entitlements.Contains(x)).ToList();
        if (requiredPlans.Length > 0 && !requiredPlans.Contains(planCode, StringComparer.OrdinalIgnoreCase)) missingFeatures.Add($"plan:{planCode}");
        var items = ParseItems(version.ItemsJson);
        var existingKeys = await db.MarketplacePackageInstallationItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => x.OriginKey).ToListAsync(ct);
        var preview = items.Select(x => new PackagePreviewItem(x.Type, x.Code, x.Name, existingKeys.Contains(Key(package.Code, x), StringComparer.OrdinalIgnoreCase) ? "IgnoreExisting" : "Create", false)).ToArray();
        return new(package.Id, version.Id, preview, missingDependencies, missingFeatures, missingDependencies.Length == 0 && missingFeatures.Count == 0 && preview.Any(x => x.Action == "Create"));
    }
    internal static PackageItemManifest[] ParseItems(string json) => JsonSerializer.Deserialize<PackageItemManifest[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    internal static string Key(string packageCode, PackageItemManifest item) => $"{packageCode}:{item.Type}:{item.Code}".ToLowerInvariant();
}

public sealed record PackageInstallationResult(Guid InstallationId, PackageInstallationStatus Status, int Applied, int Ignored, IReadOnlyList<string> Errors);

public sealed class PackageInstallationService(OrcaFacilDbContext db, PackagePreviewService previewService)
{
    public async Task<PackageInstallationResult> InstallAsync(Guid accountId, Guid userId, Guid packageId, IReadOnlySet<string> entitlements, string planCode, bool confirmed, CancellationToken ct = default)
    {
        if (!confirmed) throw new InvalidOperationException("A instalação exige prévia e confirmação explícita.");
        var preview = await previewService.PreviewAsync(accountId, packageId, entitlements, planCode, ct);
        if (!preview.CanInstall) throw new InvalidOperationException(preview.MissingFeatures.Count > 0 ? PackagePreview.IncompatiblePlanMessage : "Dependências ausentes ou pacote já instalado.");
        var package = await db.MarketplacePackages.SingleAsync(x => x.Id == packageId, ct);
        var version = await db.MarketplacePackageVersions.SingleAsync(x => x.Id == preview.VersionId, ct);
        var installation = new MarketplacePackageInstallation { AccountId = accountId, PackageId = packageId, PackageVersionId = version.Id, InstalledByUserId = userId, Status = PackageInstallationStatus.Installing, StartedAt = DateTime.UtcNow };
        db.MarketplacePackageInstallations.Add(installation);
        var errors = new List<string>(); var applied = 0; var ignored = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in PackagePreviewService.ParseItems(version.ItemsJson))
        {
            var key = PackagePreviewService.Key(package.Code, item);
            if (await db.MarketplacePackageInstallationItems.AnyAsync(x => x.AccountId == accountId && x.OriginKey == key && !x.IsDeleted, ct)) { ignored++; continue; }
            var record = new MarketplacePackageInstallationItem { AccountId = accountId, InstallationId = installation.Id, ItemType = item.Type, OriginKey = key };
            try { record.CreatedEntityId = ApplyConfiguration(accountId, item); record.Status = "Applied"; record.WasCreated = true; applied++; }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException) { record.Status = "Failed"; record.ErrorSummary = ex.Message; errors.Add($"{item.Code}: {ex.Message}"); }
            db.MarketplacePackageInstallationItems.Add(record);
        }
        installation.Status = errors.Count == 0 ? PackageInstallationStatus.Installed : applied > 0 ? PackageInstallationStatus.PartiallyInstalled : PackageInstallationStatus.Failed;
        installation.CompletedAt = DateTime.UtcNow; installation.FailureSummary = errors.Count == 0 ? null : string.Join("; ", errors);
        db.MarketplacePackageInstallationEvents.Add(new() { AccountId = accountId, InstallationId = installation.Id, ActorUserId = userId, EventType = "marketplace.package_installed", DetailsJson = JsonSerializer.Serialize(new { package.Code, version.Version, applied, ignored, errors = errors.Count }) });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(installation.Id, installation.Status, applied, ignored, errors);
    }
    private Guid ApplyConfiguration(Guid accountId, PackageItemManifest item)
    {
        if (PackageManifestValidator.ContainsUnsafeContent(item.Configuration.GetRawText())) throw new InvalidOperationException("Conteúdo inseguro bloqueado.");
        switch (item.Type.ToLowerInvariant())
        {
            case "customfield": var field = new CustomFieldDefinition(accountId, Required(item, "entityType"), item.Code, item.Name, Enum.Parse<CustomFieldType>(Required(item, "fieldType"), true)); db.CustomFieldDefinitions.Add(field); return field.Id;
            case "checklist": var checklist = new ChecklistTemplate { AccountId = accountId, Name = item.Name, TargetEntityType = Required(item, "targetEntityType"), IsActive = true }; db.ChecklistTemplates.Add(checklist); foreach (var (title, index) in Strings(item, "items").Select((x, i) => (x, i))) db.ChecklistTemplateItems.Add(new() { AccountId = accountId, ChecklistTemplateId = checklist.Id, Title = title, DisplayOrder = index + 1 }); return checklist.Id;
            case "pipeline": var pipeline = new ConfigurablePipeline { AccountId = accountId, Name = item.Name, EntityType = Required(item, "entityType"), IsActive = true }; db.ConfigurablePipelines.Add(pipeline); foreach (var (name, index) in Strings(item, "stages").Select((x, i) => (x, i))) db.ConfigurablePipelineStages.Add(new() { AccountId = accountId, PipelineId = pipeline.Id, Code = Slug(name), Name = name, DisplayOrder = index + 1, IsInitial = index == 0, IsFinal = index == Strings(item, "stages").Length - 1 }); return pipeline.Id;
            case "workflow": var workflow = new WorkflowDefinition { AccountId = accountId, EntityType = Required(item, "entityType"), Name = item.Name, IsActive = true }; var workflowVersion = new WorkflowVersion { WorkflowDefinitionId = workflow.Id, VersionNumber = 1, PublishedAt = DateTime.UtcNow }; workflow.CurrentVersionId = workflowVersion.Id; db.WorkflowDefinitions.Add(workflow); db.WorkflowVersions.Add(workflowVersion); var states = Strings(item, "states"); foreach (var (name,index) in states.Select((x,i)=>(x,i))) db.WorkflowStates.Add(new(){AccountId=accountId,WorkflowVersionId=workflowVersion.Id,Code=Slug(name),Name=name,DisplayOrder=index+1,IsInitial=index==0,IsFinal=index==states.Length-1}); return workflow.Id;
            case "automation": var automation = new AutomationRuleDefinition { AccountId = accountId, Name = item.Name, Trigger = Required(item, "trigger"), ConditionsJson = item.Configuration.TryGetProperty("conditions", out var c) ? c.GetRawText() : "[]", ActionsJson = item.Configuration.TryGetProperty("actions", out var a) ? a.GetRawText() : "[]", IsActive = false }; db.AutomationRuleDefinitions.Add(automation); return automation.Id;
            case "template": var template = new TemplateLibraryItem { AccountId = accountId, Code = item.Code, Name = item.Name, Type = Required(item, "templateType"), TargetSegment = item.Configuration.TryGetProperty("targetSegment", out var s) ? s.GetString() ?? "" : "", IsActive = true }; var templateVersion = new TemplateLibraryVersion { TemplateId = template.Id, VersionNumber = 1, ContentJson = item.Configuration.GetRawText(), PreviewText = item.Configuration.TryGetProperty("preview", out var p) ? p.GetString() ?? item.Name : item.Name, IsPublished = true }; template.CurrentVersionId = templateVersion.Id; db.TemplateLibraryItems.Add(template); db.TemplateLibraryVersions.Add(templateVersion); return template.Id;
            default: throw new InvalidOperationException($"Tipo {item.Type} ainda não pode ser aplicado com segurança.");
        }
    }
    private static string Required(PackageItemManifest item,string property)=>item.Configuration.TryGetProperty(property,out var value)&&!string.IsNullOrWhiteSpace(value.GetString())?value.GetString()!:throw new ArgumentException($"{property} é obrigatório.");
    private static string[] Strings(PackageItemManifest item,string property)=>item.Configuration.TryGetProperty(property,out var value)&&value.ValueKind==JsonValueKind.Array?value.EnumerateArray().Select(x=>x.GetString()).Where(x=>!string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray():[];
    private static string Slug(string value)=>string.Concat(value.Normalize(System.Text.NormalizationForm.FormD).Where(c=>System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)!=System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Replace(' ','-');
}

public sealed class PackageRollbackService(OrcaFacilDbContext db)
{
    public const string ImpactMessage = "O rollback desativa configurações instaladas pelo pacote, mas não apaga dados operacionais já criados pelos usuários.";
    public async Task RollbackAsync(Guid accountId, Guid userId, Guid installationId, bool preserveOperationalDataConfirmed, CancellationToken ct=default)
    {
        if (!preserveOperationalDataConfirmed) throw new InvalidOperationException("Confirme que compreendeu o impacto do rollback.");
        var installation=await db.MarketplacePackageInstallations.SingleOrDefaultAsync(x=>x.Id==installationId&&x.AccountId==accountId&&!x.IsDeleted,ct)??throw new KeyNotFoundException("Instalação não encontrada.");
        if (installation.Status is not (PackageInstallationStatus.Installed or PackageInstallationStatus.PartiallyInstalled)) throw new InvalidOperationException("A instalação não pode ser revertida neste estado.");
        var items=await db.MarketplacePackageInstallationItems.Where(x=>x.InstallationId==installation.Id&&x.AccountId==accountId&&x.WasCreated&&!x.IsDeactivated).ToListAsync(ct);
        foreach(var item in items){ if(item.CreatedEntityId is not Guid id)continue; switch(item.ItemType.ToLowerInvariant()){case "customfield": var f=await db.CustomFieldDefinitions.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(f is not null)f.IsActive=false;break;case "checklist":var c=await db.ChecklistTemplates.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(c is not null)c.IsActive=false;break;case "pipeline":var p=await db.ConfigurablePipelines.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(p is not null)p.IsActive=false;break;case "workflow":var w=await db.WorkflowDefinitions.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(w is not null)w.IsActive=false;break;case "automation":var a=await db.AutomationRuleDefinitions.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(a is not null)a.IsActive=false;break;case "template":var t=await db.TemplateLibraryItems.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==accountId,ct);if(t is not null)t.IsActive=false;break;}item.IsDeactivated=true;item.Status="RolledBack"; }
        installation.Status=PackageInstallationStatus.RolledBack; db.MarketplacePackageInstallationEvents.Add(new(){AccountId=accountId,InstallationId=installation.Id,ActorUserId=userId,EventType="marketplace.package_rolled_back",DetailsJson=JsonSerializer.Serialize(new{items=items.Count,preservesOperationalData=true})});await db.SaveChangesAsync(ct);
    }
}
