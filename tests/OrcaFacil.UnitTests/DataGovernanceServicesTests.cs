using OrcaFacil.Application.DataGovernance;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DataQualityRuleServiceTests
{
    [Fact] public void Global_rule_requires_superadmin() => Assert.Throws<UnauthorizedAccessException>(() => new DataQualityRuleService().CreateVersion(new("GLOBAL_X", "x", "Client", "name", QualitySeverity.High, true, false, "review"), new("GLOBAL_X", "y", "Client", "name", QualitySeverity.High, true, false, "review"), false));
}

public sealed class DataQualityEngineTests
{
    [Fact] public void Evaluates_only_current_account()
    {
        var tenant = Guid.NewGuid(); var other = Guid.NewGuid();
        var rows = new[] { Row(tenant, Guid.NewGuid(), ("document", null)), Row(other, Guid.NewGuid(), ("document", null)) };
        var findings = new DataQualityEngine().Evaluate(tenant, rows, new DataQualityRuleService().InitialRules);
        Assert.Single(findings); Assert.All(findings, x => Assert.Equal(tenant, x.AccountId));
    }
    internal static GovernedRecord Row(Guid account, Guid id, params (string Key, string? Value)[] values) => new(account, id, "Client", values.ToDictionary(x => x.Key, x => x.Value));
}

public sealed class DataQualityScoreServiceTests
{
    [Fact] public void Score_is_deterministic_and_explainable()
    {
        var finding = new QualityFinding(Guid.NewGuid(), Guid.NewGuid(), "Client", "X", QualitySeverity.High, "problem", "review");
        var service = new DataQualityScoreService(); var first = service.Calculate(10, [finding]); var second = service.Calculate(10, [finding]);
        Assert.Equal(first.Value, second.Value); Assert.Equal(first.WeightedIssues, second.WeightedIssues); Assert.Equal(95, first.Value); Assert.Equal(10, first.WeightedIssues);
    }
}

public sealed class DuplicateDetectionServiceTests
{
    [Fact] public void Same_document_is_detected_only_inside_tenant()
    {
        var tenant = Guid.NewGuid(); var a = Guid.NewGuid(); var b = Guid.NewGuid();
        var rows = new[] { DataQualityEngineTests.Row(tenant, a, ("document", "123.456"), ("name", "Empresa A")), DataQualityEngineTests.Row(tenant, b, ("document", "123456"), ("name", "Empresa B")), DataQualityEngineTests.Row(Guid.NewGuid(), Guid.NewGuid(), ("document", "123456")) };
        var candidate = Assert.Single(new DuplicateDetectionService().Detect(tenant, rows));
        Assert.Equal(a, candidate.LeftId); Assert.Equal(b, candidate.RightId); Assert.Contains("document", candidate.MatchedFields);
    }
}

public sealed class MasterDataMergeServiceTests
{
    [Fact] public void Merge_requires_preview_permission_confirmation_and_reason()
    {
        var tenant = Guid.NewGuid(); var service = new MasterDataMergeService();
        var preview = service.Preview(DataQualityEngineTests.Row(tenant, Guid.NewGuid(), ("name", "Principal")), DataQualityEngineTests.Row(tenant, Guid.NewGuid(), ("name", "Secundário")));
        Assert.Throws<UnauthorizedAccessException>(() => service.Confirm(preview, "cadastros repetidos", false, true));
        Assert.Throws<InvalidOperationException>(() => service.Confirm(preview, "cadastros repetidos", true, false));
        Assert.Throws<ArgumentException>(() => service.Confirm(preview, "", true, true));
        var decision = service.Confirm(preview, "cadastros repetidos", true, true);
        Assert.False(decision.PhysicallyDeleted);
    }
    [Fact] public void Cross_tenant_merge_is_rejected() => Assert.Throws<InvalidOperationException>(() => new MasterDataMergeService().Preview(DataQualityEngineTests.Row(Guid.NewGuid(), Guid.NewGuid(), ("name", "A")), DataQualityEngineTests.Row(Guid.NewGuid(), Guid.NewGuid(), ("name", "B"))));
}

public sealed class ClientMergeServiceTests
{
    [Fact] public void Preview_preserves_relationship_counts() { var tenant = Guid.NewGuid(); var preview = new ClientMergeService(new MasterDataMergeService()).Preview(DataQualityEngineTests.Row(tenant, Guid.NewGuid(), ("name", "A")), DataQualityEngineTests.Row(tenant, Guid.NewGuid(), ("name", "B")), new Dictionary<string, int> { ["quotes"] = 3 }); Assert.Equal(3, preview.RelatedRecords["quotes"]); }
}

public sealed class DataNormalizationServiceTests
{
    [Fact] public void Sensitive_normalization_preserves_original_in_preview() { var result = new DataNormalizationService().Preview(" 123.456 ", NormalizationKind.Document); Assert.Equal(" 123.456 ", result.Original); Assert.Equal("123456", result.Normalized); Assert.True(result.Sensitive); }
}

public sealed class DataImportServiceTests
{
    [Fact] public void Preview_validates_each_row() { var preview = new DataImportService(new DataImportPreviewService()).Preview(Guid.NewGuid(), [new Dictionary<string, string?> { ["name"] = null }], ["name"]); Assert.Single(preview.Rows[0].Errors); }
}

public sealed class DataImportCommitServiceTests
{
    [Fact] public void Commit_without_preview_is_rejected() => Assert.Throws<InvalidOperationException>(() => new DataImportCommitService().Commit(Guid.NewGuid(), null, true));
    [Fact] public void Invalid_row_is_not_imported() { var account = Guid.NewGuid(); var preview = new DataImportPreviewService().Create(account, [new Dictionary<string, string?> { ["name"] = null }, new Dictionary<string, string?> { ["name"] = "Cliente" }], ["name"]); var result = new DataImportCommitService().Commit(account, preview, true); Assert.Equal(1, result.Imported); Assert.Equal(1, result.Skipped); }
}

public sealed class DataImportRollbackServiceTests
{
    [Fact] public void Rollback_is_blocked_after_later_change() { var committed = DateTime.UtcNow.AddMinutes(-5); Assert.Throws<InvalidOperationException>(() => new DataImportRollbackService().EnsureSafe(committed, DateTime.UtcNow)); }
}

public sealed class SensitiveDataChangeReviewServiceTests
{
    [Fact] public void Sensitive_value_is_masked() { var masked = new SensitiveDataChangeReviewService().Mask("12345678901"); Assert.Equal("•••••••8901", masked); Assert.DoesNotContain("1234567", masked); }
}

public sealed class DataQualityPermissionTests
{
    [Fact] public void Bulk_sensitive_change_requires_permission() => Assert.Throws<UnauthorizedAccessException>(() => new SensitiveDataChangeReviewService().EnsureBulkPermission(2, false));
}

public sealed class DataQualityTenantIsolationTests
{
    [Fact] public void Import_preview_cannot_be_committed_by_another_tenant() { var owner = Guid.NewGuid(); var preview = new DataImportPreviewService().Create(owner, [new Dictionary<string, string?> { ["name"] = "A" }], ["name"]); Assert.Throws<UnauthorizedAccessException>(() => new DataImportCommitService().Commit(Guid.NewGuid(), preview, true)); }
}
