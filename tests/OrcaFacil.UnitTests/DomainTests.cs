using OrcaFacil.Application.Plans;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Domain.ValueObjects;
using OrcaFacil.Infrastructure.Pdf;
using Xunit;

namespace OrcaFacil.UnitTests;

public class DomainTests
{
    [Fact]
    public void Email_Normalizes_Value()
    {
        var email = new Email(" USER@Example.COM ");
        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void PublicToken_Generates_Safe_Token()
    {
        var token = new PublicToken();
        Assert.True(token.Value.Length >= 32);
    }

    [Fact]
    public void Document_Calculates_Totals()
    {
        var document = new Document { ClientName = "Cliente" };
        document.Items.Add(new DocumentItem { Description = "Serviço", Quantity = 2, UnitPrice = 10, Discount = 1 });
        document.CalculateTotals();
        Assert.Equal(19, document.Total);
    }

    [Fact]
    public void Budget_Approved_Can_Be_Converted_To_Receipt()
    {
        var document = new Document { Type = DocumentType.Budget, ClientName = "Cliente", ClientDecision = ClientDecision.Approved };
        document.IssueNumber("ORC-1");
        var receipt = document.ConvertToReceipt("REC-1");
        Assert.Equal(DocumentType.Receipt, receipt.Type);
    }

    [Fact]
    public void Number_To_Words_Returns_Currency_Text()
    {
        var service = new NumberToWordsPtBrService();
        Assert.Equal("um real e dois centavos", service.ToCurrencyWords(1.02m));
    }

    [Fact]
    public void Free_Plan_Has_Watermark()
    {
        var service = new PlanLimitService();
        Assert.True(service.PdfHasWatermark(PlanType.Free));
        Assert.False(service.PdfHasWatermark(PlanType.Pro));
    }
}

public class BrazilianDocumentTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Cpf_Valid_CheckDigits_Are_Accepted(string value) =>
        Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));

    [Theory]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("123")]
    public void Cpf_Invalid_CheckDigits_Are_Rejected(string value) =>
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));

    [Theory]
    [InlineData("04.252.011/0001-10")]
    [InlineData("04252011000110")]
    public void Cnpj_Valid_CheckDigits_Are_Accepted(string value) =>
        Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, value));

    [Theory]
    [InlineData("04252011000111")]
    [InlineData("00000000000000")]
    [InlineData("123")]
    public void Cnpj_Invalid_CheckDigits_Are_Rejected(string value) =>
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Optional_Documents_Are_Accepted(string? value) =>
        Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));
}
