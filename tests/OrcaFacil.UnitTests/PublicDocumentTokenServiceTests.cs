using OrcaFacil.Application.Documents;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class PublicDocumentTokenServiceTests
{
    [Fact]
    public void Creates_unpredictable_url_safe_token_and_stores_only_hash()
    {
        var service = new PublicDocumentTokenService();
        var first = service.Create();
        var second = service.Create();
        Assert.NotEqual(first.Token, second.Token);
        Assert.DoesNotContain("/", first.Token);
        Assert.DoesNotContain("+", first.Token);
        Assert.NotEqual(first.Token, first.Hash);
        Assert.True(service.Matches(first.Token, first.Hash));
        Assert.False(service.Matches(second.Token, first.Hash));
    }
}
