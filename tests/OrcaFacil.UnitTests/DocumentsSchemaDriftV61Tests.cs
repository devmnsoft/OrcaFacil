using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DocumentsTemplateCodeSchemaTests
{
    [Fact]
    public void Ef_Maps_TemplateCode_To_Required_Varchar40()
    {
        using var db = new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options);
        var property = db.Model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.TemplateCode))!;
        Assert.Equal(typeof(string), property.ClrType);
        Assert.Equal("template_code", property.GetColumnName());
        Assert.Equal(40, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Diagnostics_Requires_TemplateCode() =>
        Assert.Equal("character varying", DatabaseDiagnosticsService.RequiredDocumentColumns["template_code"]);
}

public sealed class DocumentsFullSchemaTests
{
    [Theory]
    [InlineData("template_snapshot", "jsonb")]
    [InlineData("row_version", "bytea")]
    [InlineData("payment_method", "character varying")]
    [InlineData("deposit_amount", "numeric")]
    [InlineData("conditions_text", "text")]
    [InlineData("client_snapshot", "jsonb")]
    public void Diagnostics_Covers_Ef_Commercial_Contract(string column, string expectedType) =>
        Assert.Equal(expectedType, DatabaseDiagnosticsService.RequiredDocumentColumns[column]);

    [Theory]
    [InlineData("document_revisions")]
    [InlineData("budget_templates")]
    [InlineData("budget_template_items")]
    public void Diagnostics_Covers_Critical_Commercial_Table(string table) =>
        Assert.Contains(table, DatabaseDiagnosticsService.RequiredTables);
}
