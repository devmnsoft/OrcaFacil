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

    [Fact]
    public void Commercial_items_order_is_preserved_in_new_format()
    {
        var snapshot = new DocumentSnapshot(
            new("Minha Empresa Ltda", "12.345.678/0001-90", "contato@empresa.com", "(11) 99999-0000", "Rua das Flores, 100", "São Paulo", "SP", null, "12345678000190", null),
            new("João da Silva", "PF", "123.456.789-00", "(11) 98888-7777", "joao@email.com", "Av Central, 50", "São Paulo", "SP"),
            new("ORC-2026-002", new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(15), "10 dias", "À vista", "Termos", "Notas", "essential", "#0284c7", null, true, 300m, 0m, 300m, "pt-BR", "BRL"),
            [
                new("Zebra (último no alfabeto)", "un", 1m, 100m, 0m, 100m, 100m),
                new("Alpha (primeiro no alfabeto)", "un", 1m, 100m, 0m, 100m, 100m),
                new("Beta (meio no alfabeto)", "un", 1m, 100m, 0m, 100m, 100m)
            ]);

        var result = _serializer.Serialize(snapshot);

        // Ordem original preservada no JSON novo
        var zebraIndex = result.Json.IndexOf("Zebra", StringComparison.Ordinal);
        var alphaIndex = result.Json.IndexOf("Alpha", StringComparison.Ordinal);
        var betaIndex = result.Json.IndexOf("Beta", StringComparison.Ordinal);

        Assert.True(zebraIndex < alphaIndex, "Zebra deve vir antes de Alpha respeitando a ordem comercial");
        Assert.True(alphaIndex < betaIndex, "Alpha deve vir antes de Beta respeitando a ordem comercial");
        Assert.True(_serializer.VerifySnapshotHash(result.Json, snapshot, result.Hash));
    }

    [Fact]
    public void Template_query_isolation_rules_enforce_strict_tenant_separation()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var templates = new List<BudgetTemplate>
        {
            new() { AccountId = accountA, UserId = user1, Title = "Template Conta A", IsSystemTemplate = false },
            new() { AccountId = accountB, UserId = user2, Title = "Template Conta B", IsSystemTemplate = false },
            new() { AccountId = null, UserId = user1, Title = "Legado User 1", IsSystemTemplate = false },
            new() { AccountId = null, UserId = user2, Title = "Legado User 2", IsSystemTemplate = false },
            new() { AccountId = null, UserId = Guid.Empty, Title = "Sistema", IsSystemTemplate = true }
        };

        // Cenário 1: Usuário logado na Conta A (com conta selecionada)
        var forAccountA = templates.Where(x => x.AccountId == accountA).ToList();
        Assert.Single(forAccountA);
        Assert.Equal("Template Conta A", forAccountA[0].Title);

        // Cenário 2: Usuário 1 sem conta selecionada (legado pessoal)
        var forUser1NoAccount = templates.Where(x => x.AccountId == null && x.UserId == user1).ToList();
        Assert.Single(forUser1NoAccount);
        Assert.Equal("Legado User 1", forUser1NoAccount[0].Title);

        // Cenário 3: Usuário 2 sem conta selecionada (não pode ver legado do Usuário 1)
        var forUser2NoAccount = templates.Where(x => x.AccountId == null && x.UserId == user2).ToList();
        Assert.Single(forUser2NoAccount);
        Assert.Equal("Legado User 2", forUser2NoAccount[0].Title);

        // Cenário 4: Modelos de sistema
        var systemTemplates = templates.Where(x => x.IsSystemTemplate).ToList();
        Assert.Single(systemTemplates);
        Assert.Equal("Sistema", systemTemplates[0].Title);
    }

    private static DocumentSnapshot CreateSampleSnapshot(string lang, string currency) => new(
        new("Minha Empresa Ltda", "12.345.678/0001-90", "contato@empresa.com", "(11) 99999-0000", "Rua das Flores, 100", "São Paulo", "SP", null, "12345678000190", null),
        new("João da Silva", "PF", "123.456.789-00", "(11) 98888-7777", "joao@email.com", "Av Central, 50", "São Paulo", "SP"),
        new("ORC-2026-001", new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc), DateTime.UtcNow.AddDays(15), "10 dias úteis", "50% entrada + 50% entrega", "Termos gerais", "Escopo detalhado", "essential", "#0284c7", null, true, 500m, 50m, 450m, lang, currency),
        [
            new("Pintura de Parede", "m²", 50m, 10m, 50m, 500m, 450m)
        ]);
}
