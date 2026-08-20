using OrcaFacil.Application.Security;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class Sprint19SecurityContractTests
{
    [Theory]
    [InlineData(PermissionCodes.SearchGlobal)]
    [InlineData(PermissionCodes.CommandCenterUse)]
    [InlineData(PermissionCodes.AssistantUse)]
    [InlineData(PermissionCodes.KnowledgeBaseView)]
    [InlineData(PermissionCodes.FavoritesManageOwn)]
    public void Productivity_permissions_are_canonical(string permission)
    {
        Assert.Contains(permission, PermissionCodes.All);
    }

    [Fact]
    public void Database_patch_is_tenant_safe_and_idempotent()
    {
        var root = FindRepositoryRoot();
        var patch = File.ReadAllText(Path.Combine(root, "database", "patch_release_candidate_schema.sql"));
        Assert.Contains("Search.Global", patch, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (code) DO NOTHING", patch, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", patch, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rule_assistant_remains_read_only_and_honest()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Web", "Services", "InternalAssistantService.cs"));
        Assert.Contains("Resposta baseada nas regras do OrçaFácil", source, StringComparison.Ordinal);
        Assert.Contains("AccountId == accountId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
