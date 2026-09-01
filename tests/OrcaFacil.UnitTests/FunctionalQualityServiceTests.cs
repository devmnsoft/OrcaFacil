using OrcaFacil.Application.Quality;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FunctionalQualityServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"orcafacil-quality-{Guid.NewGuid():N}");

    [Fact]
    public void SourceAudit_ReturnsRealFileAndLineForBlockingPattern()
    {
        var source = Path.Combine(root, "src");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "Broken.cs"), "public class Broken\n{\n void Run() => throw new Not" + "ImplementedException();\n}");

        var finding = Assert.Single(new SourceCodeFindingService(root).Scan());

        Assert.Equal("src/Broken.cs", finding.File);
        Assert.Equal(3, finding.Line);
        Assert.Equal(FindingSeverity.P0, finding.Severity);
    }

    [Fact]
    public void Readiness_IsCriticalWhenModuleContainsP0()
    {
        var pageDirectory = Path.Combine(root, "src", "OrcaFacil.Web", "Pages", "Clients");
        Directory.CreateDirectory(pageDirectory);
        File.WriteAllText(Path.Combine(pageDirectory, "Index.cshtml"), "@page");
        File.WriteAllText(Path.Combine(pageDirectory, "Index.cshtml.cs"), "[Authorize] class Page { Guid AccountId; }");
        var findings = new[] { new SourceCodeFinding("src/OrcaFacil.Web/Pages/Clients/Index.cshtml", 1, FindingSeverity.P0, "test", "risk", "fix") };

        var result = new ModuleReadinessService(root, new BusinessRuleAuditService()).Evaluate(findings, DateTimeOffset.UnixEpoch);

        Assert.Equal(QualityStatus.Critical, result.Single(x => x.Module == "Clientes").Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
