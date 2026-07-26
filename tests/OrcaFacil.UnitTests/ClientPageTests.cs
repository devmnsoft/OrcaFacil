using OrcaFacil.Domain.Enums;
using OrcaFacil.Web.Pages.Clients;
using Xunit;

namespace OrcaFacil.UnitTests;

public class ClientPageTests
{
    [Fact]
    public void Clients_Index_Razor_Has_Single_Page_Directive_And_Strong_Model()
    {
        var content = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "OrcaFacil.Web", "Pages", "Clients", "Index.cshtml"));
        var lines = content.Replace("\r\n", "\n").Split('\n');

        Assert.Equal("@page", lines[0]);
        Assert.Equal("@model OrcaFacil.Web.Pages.Clients.IndexModel", lines[1]);
        Assert.Single(lines.Where(line => System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*@page(\s|$)")));
        Assert.DoesNotContain("@model dynamic", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nameof(IndexModel)", content, StringComparison.Ordinal);
        Assert.DoesNotContain("class IndexModel", content, StringComparison.Ordinal);
        Assert.Contains("asp-page=\"/Clients/Index\"", content, StringComparison.Ordinal);
        Assert.Contains("asp-page-handler=\"Delete\"", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(BrazilianDocumentType.CPF, "52998224725", "***.***.***-25")]
    [InlineData(BrazilianDocumentType.CNPJ, "04252011000110", "**.***.***/****-10")]
    public void Clients_Index_Masks_Documents(BrazilianDocumentType type, string number, string expected) =>
        Assert.Equal(expected, IndexModel.Mask(type, number));

    [Fact]
    public void Clients_IndexModel_Exposes_Filter_Pagination_And_Delete_Handler()
    {
        var modelType = typeof(IndexModel);

        Assert.True(modelType.IsSealed);
        Assert.NotNull(modelType.GetProperty(nameof(IndexModel.Search)));
        Assert.NotNull(modelType.GetProperty(nameof(IndexModel.Document)));
        Assert.NotNull(modelType.GetProperty(nameof(IndexModel.PersonType)));
        Assert.NotNull(modelType.GetProperty(nameof(IndexModel.City)));
        Assert.NotNull(modelType.GetProperty(nameof(IndexModel.PageNumber)));
        Assert.NotNull(modelType.GetMethod(nameof(IndexModel.OnPostDeleteAsync)));
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrcaFacil.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
