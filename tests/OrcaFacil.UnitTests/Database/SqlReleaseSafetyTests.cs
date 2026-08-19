using System.Text.RegularExpressions;
using Xunit;

namespace OrcaFacil.UnitTests.Database;

public sealed class SqlReleaseSafetyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("database/script_completop.sql")]
    [InlineData("database/patch_release_candidate_schema.sql")]
    [InlineData("database/patch_fix_account_onboarding_states.sql")]
    [InlineData("database/seed-superadmin.sql")]
    public void ReleaseScripts_DoNotContainDestructiveTableOperations(string relativePath)
    {
        var sql = Read(relativePath);

        Assert.DoesNotMatch(new Regex(@"\b(?:DROP\s+(?:TABLE|SCHEMA|DATABASE)|TRUNCATE)\b", RegexOptions.IgnoreCase), sql);
    }

    [Fact]
    public void CompleteSchema_ContainsEveryRequiredProductionTable()
    {
        var sql = Read("database/script_completop.sql");
        string[] tables =
        [
            "users", "business_accounts", "account_members", "account_onboarding_states", "clients",
            "service_catalog_items", "documents", "document_items", "public_quotes", "work_orders",
            "manual_payments", "receipts", "notifications", "audit_logs", "system_logs",
            "email_outbox_messages", "plans", "plan_versions", "features", "plan_feature_values", "subscriptions"
        ];

        foreach (var table in tables)
            Assert.Matches($@"(?i)CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+orcafacil\.{table}\b", sql);
    }

    [Fact]
    public void SuperAdminSeed_IsIdempotent_AndDoesNotReplaceCredentials()
    {
        var sql = Read("database/seed-superadmin.sql");

        Assert.Contains("WHERE NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ON CONFLICT DO UPDATE SET password_hash", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must_change_password", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthenticationPatch_IsAdditive_AndIncludesSecurityState()
    {
        var sql = Read("database/patch_release_candidate_schema.sql");

        Assert.Contains("ADD COLUMN IF NOT EXISTS failed_login_attempts", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ADD COLUMN IF NOT EXISTS session_version", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE IF NOT EXISTS orcafacil.account_onboarding_states", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(RepositoryRoot, relativePath));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
                return directory.FullName;

        throw new DirectoryNotFoundException("Não foi possível localizar a raiz do repositório OrcaFacil.");
    }
}
