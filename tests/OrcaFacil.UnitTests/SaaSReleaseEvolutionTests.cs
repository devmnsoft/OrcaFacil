using System.Text.Json;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Localization;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class SaaSReleaseEvolutionTests
{
    private readonly DocumentSnapshotSerializer _serializer = new();

    [Fact]
    public void Historical_legacy_and_current_snapshots_validate_hashes_correctly()
    {
        var legacySnapshot = CreateSampleSnapshot("pt-BR", "BRL");
        var legacySerialized = _serializer.SerializeLegacy(legacySnapshot);

        Assert.True(_serializer.IsHistoricalFormat(legacySerialized.Json));
        Assert.True(_serializer.VerifySnapshotHash(legacySerialized.Json, legacySnapshot, legacySerialized.Hash));

        var currentSnapshot = CreateSampleSnapshot("en-US", "BRL");
        var currentSerialized = _serializer.Serialize(currentSnapshot);

        Assert.False(_serializer.IsHistoricalFormat(currentSerialized.Json));
        Assert.True(_serializer.VerifySnapshotHash(currentSerialized.Json, currentSnapshot, currentSerialized.Hash));
    }

    [Fact]
    public void Tampered_snapshot_is_strictly_rejected()
    {
        var original = CreateSampleSnapshot("pt-BR", "BRL");
        var serialized = _serializer.Serialize(original);

        // Adulterando valor total
        var tamperedQuote = original.Quote with { Total = original.Quote.Total + 10m };
        var tamperedSnapshot = original with { Quote = tamperedQuote };

        Assert.False(_serializer.VerifySnapshotHash(serialized.Json, tamperedSnapshot, serialized.Hash));
    }

    [Fact]
    public void Canonical_rounding_produces_identical_subtotals_and_totals()
    {
        var item = new DocumentItem
        {
            Quantity = 2.5m,
            UnitPrice = 33.33m,
            Discount = 5.00m
        };

        // Quantidade (2.5) * Preço (33.33) = 83.325 -> Arredondado = 83.33. Menos desconto (5.00) = 78.33
        var total = item.CalculateTotal();
        Assert.Equal(78.33m, total);

        var doc = new Document
        {
            Type = DocumentType.Budget,
            Discount = 3.33m
        };
        doc.Items.Add(item);
        doc.CalculateTotals();

        Assert.Equal(78.33m, doc.Subtotal);
        Assert.Equal(3.33m, doc.Discount);
        Assert.Equal(75.00m, doc.Total);
    }

    [Fact]
    public void Localization_records_missing_key_even_when_falling_back_to_portuguese()
    {
        var tracker = new InMemoryMissingKeyTracker();
        var localizer = new JsonTextLocalizer(tracker);

        // Chave inexistente no catálogo
        var result = localizer.GetForCulture("NonExistent.Key.ForTest", "en-US");

        Assert.Equal("NonExistent.Key.ForTest", result);
        var missing = tracker.GetMissingKeys();
        Assert.Contains(missing, m => m.Key == "NonExistent.Key.ForTest" && m.CultureCode == "en-US");
    }

    [Fact]
    public async Task Commercial_ai_reviewer_identifies_missing_data_without_altering_document()
    {
        var reviewer = new CommercialAiReviewer();
        var doc = new Document
        {
            AccountId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = DocumentType.Budget,
            Status = "Draft",
            ClientName = "" // Sem cliente
        };
        var itemWithZeroPrice = new DocumentItem
        {
            Description = "Serviço sem preço",
            Quantity = 1,
            UnitPrice = 0,
            Discount = 0
        };

        var context = new AiRequestContext(doc.AccountId.Value, doc.UserId, new HashSet<string> { "documents.edit" });
        var policy = new AiGovernancePolicy(doc.AccountId.Value);

        var review = await reviewer.ReviewQuoteAsync(context, policy, doc, [itemWithZeroPrice], null);

        Assert.True(review.Succeeded);
        Assert.NotEmpty(review.Findings);
        Assert.Contains(review.Findings, f => f.Category == "Cliente" || f.Category == "Geral");
        Assert.Contains(review.Findings, f => f.Category == "Valores");
        Assert.Equal(0, itemWithZeroPrice.UnitPrice); // Preços intactos
    }

    private static DocumentSnapshot CreateSampleSnapshot(string lang, string currency) => new(
        new("Minha Empresa Ltda", "12.345.678/0001-90", "contato@empresa.com", "(11) 99999-0000", "Rua das Flores, 100", "São Paulo", "SP", null, "12345678000190", null),
        new("João da Silva", "PF", "123.456.789-00", "(11) 98888-7777", "joao@email.com", "Av Central, 50", "São Paulo", "SP"),
        new("ORC-2026-001", new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(15), "10 dias úteis", "50% entrada + 50% entrega", "Termos gerais", "Escopo detalhado", "essential", "#0284c7", null, true, 500m, 50m, 450m, lang, currency),
        [
            new("Pintura de Parede", "m²", 50m, 10m, 50m, 500m, 450m)
        ]);
}
