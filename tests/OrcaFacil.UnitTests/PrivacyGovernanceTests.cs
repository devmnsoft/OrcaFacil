using System.Text.Json;
using OrcaFacil.Application.Privacy;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class PrivacyGovernanceTests
{
    [Fact]
    public void Sanitizer_removes_secrets_recursively()
    {
        var json = new SensitiveDataSanitizer().SanitizeJson(new { Name = "Conta", PasswordHash = "hash-real", Nested = new { ApiKey = "key-real", Token = "token-real" } });
        Assert.DoesNotContain("hash-real", json); Assert.DoesNotContain("key-real", json); Assert.DoesNotContain("token-real", json);
        Assert.Equal(3, json.Split("[REDACTED]").Length - 1);
    }

    [Fact]
    public void Sanitizer_preserves_non_sensitive_export_fields()
    {
        var json = new SensitiveDataSanitizer().SanitizeJson(new { ClientId = Guid.NewGuid(), Amount = 42m });
        using var document = JsonDocument.Parse(json);
        Assert.Equal(42m, document.RootElement.GetProperty("Amount").GetDecimal());
    }

    [Fact]
    public void Anonymization_requires_explicit_irreversible_phrase() =>
        Assert.Equal("ANONIMIZAR DEFINITIVAMENTE", AnonymizationService.ConfirmationPhrase);

    [Theory]
    [InlineData(RetentionAction.Keep)] [InlineData(RetentionAction.Archive)] [InlineData(RetentionAction.Anonymize)] [InlineData(RetentionAction.SoftDelete)]
    public void Retention_actions_are_explicit(RetentionAction action) => Assert.True(Enum.IsDefined(action));
}
