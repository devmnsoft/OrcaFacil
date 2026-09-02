using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Persistence.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class DocumentsDepositAmountSchemaTests
{
    [Fact]
    public void DepositAmountIsNullableNumericWithExpectedPrecision()
    {
        using var db = new OrcaFacilDbContext(new DbContextOptionsBuilder<OrcaFacilDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .Options);
        var property = db.Model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.DepositAmount))!;

        Assert.True(property.IsNullable);
        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
        Assert.Equal("deposit_amount", property.GetColumnName());
    }

    [Fact]
    public void DiagnosticsContractRequiresDepositAmount() =>
        Assert.Equal("numeric", DatabaseSchemaContractService.RegistrationContract["documents"]["deposit_amount"]);
}
