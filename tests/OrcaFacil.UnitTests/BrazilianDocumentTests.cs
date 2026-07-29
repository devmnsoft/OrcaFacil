using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class BrazilianDocumentTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void Accepts_valid_cpf_with_or_without_mask(string value) =>
        Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));

    [Theory]
    [InlineData("52998224724")]
    [InlineData("111.111.111-11")]
    [InlineData("52998224735")] // primeiro dígito verificador incorreto
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("5299822472A")]
    public void Rejects_invalid_repeated_or_malformed_cpf(string value) =>
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    public void Accepts_valid_cnpj_with_or_without_mask(string value) =>
        Assert.True(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, value));

    [Theory]
    [InlineData("11222333000180")]
    [InlineData("11.111.111/1111-11")]
    [InlineData("11222333000171")] // primeiro dígito verificador incorreto
    [InlineData("1122233300018")]
    [InlineData("112223330001810")]
    [InlineData("1122233300018A")]
    public void Rejects_invalid_repeated_or_malformed_cnpj(string value) =>
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, value));

    [Fact]
    public void Normalization_keeps_only_digits() =>
        Assert.Equal("11222333000181", BrazilianDocument.Normalize(" 11.222.333/0001-81 "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_missing_document(string? value)
    {
        Assert.False(BrazilianDocument.HasBasicValidLength(BrazilianDocumentType.CPF, value));
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CPF, value));
        Assert.False(BrazilianDocument.HasBasicValidLength(BrazilianDocumentType.CNPJ, value));
        Assert.False(BrazilianDocument.HasValidCheckDigits(BrazilianDocumentType.CNPJ, value));
    }
}
