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
    [Fact]
    public void Cpf_With_Valid_Check_Digits_Is_Accepted()
        => Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, "529.982.247-25"));

    [Fact]
    public void Cpf_With_Invalid_Check_Digits_Is_Rejected()
        => Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, "52998224724"));

    [Fact]
    public void Cpf_With_Repeated_Digits_Is_Rejected()
        => Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, "11111111111"));

    [Fact]
    public void Cnpj_With_Valid_Check_Digits_Is_Accepted()
        => Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, "11.222.333/0001-81"));

    [Fact]
    public void Cnpj_With_Invalid_Check_Digits_Is_Rejected()
        => Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, "11222333000182"));

    [Fact]
    public void Cnpj_With_Repeated_Digits_Is_Rejected()
        => Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, "00000000000000"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Optional_Document_Is_Accepted(string? value)
        => Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));
}
