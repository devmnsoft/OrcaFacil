using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Localization;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Infrastructure.Pdf;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class MultilingualJourneyAndHomologationTests
{
    [Fact]
    public void NumberToWords_Multilingual_MaintainsBrlAndNeverConvertsToDollars()
    {
        var service = new NumberToWordsPtBrService();

        // 1. pt-BR
        var ptBrText = service.ToCurrencyWords(150.50m, "pt-BR", "BRL").ToLowerInvariant();
        Assert.Contains("reais", ptBrText);
        Assert.Contains("cinquenta centavos", ptBrText);
        Assert.DoesNotContain("dólar", ptBrText);
        Assert.DoesNotContain("dollar", ptBrText);

        // 2. en-US with BRL currency (Critical requirement: switching UI must never change currency to USD)
        var enUsBrlText = service.ToCurrencyWords(150.50m, "en-US", "BRL").ToLowerInvariant();
        Assert.Contains("brazilian reals", enUsBrlText);
        Assert.Contains("cents", enUsBrlText);
        Assert.DoesNotContain("dollar", enUsBrlText);

        // 3. es-ES with BRL currency
        var esEsBrlText = service.ToCurrencyWords(150.50m, "es-ES", "BRL").ToLowerInvariant();
        Assert.Contains("reales brasileños", esEsBrlText);
        Assert.Contains("centavos", esEsBrlText);
        Assert.DoesNotContain("dólar", esEsBrlText);

        // 4. en-US with USD currency (when explicitly USD)
        var enUsUsdText = service.ToCurrencyWords(100m, "en-US", "USD").ToLowerInvariant();
        Assert.Contains("dollars", enUsUsdText);
    }

    [Fact]
    public void JsonTextLocalizer_LoadsCatalogs_AndTranslatesAcrossFourCultures()
    {
        // Identifica o caminho dos catálogos JSON
        var baseDir = AppContext.BaseDirectory;
        var localizationDir = Path.Combine(baseDir, "Localization");
        if (!Directory.Exists(localizationDir))
        {
            localizationDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "OrcaFacil.Web", "Localization"));
        }

        var tracker = new InMemoryMissingKeyTracker();
        var localizer = new JsonTextLocalizer(tracker, localizationDir);

        // pt-BR
        Assert.Equal("Salvar", localizer.GetForCulture("Common.Save", "pt-BR"));
        Assert.Equal("Orçamentos", localizer.GetForCulture("Nav.Quotes", "pt-BR"));

        // en-US
        Assert.Equal("Save", localizer.GetForCulture("Common.Save", "en-US"));
        Assert.Equal("Quotes", localizer.GetForCulture("Nav.Quotes", "en-US"));

        // es-ES
        Assert.Equal("Guardar", localizer.GetForCulture("Common.Save", "es-ES"));
        Assert.Equal("Presupuestos", localizer.GetForCulture("Nav.Quotes", "es-ES"));

        // es-419
        Assert.Equal("Guardar", localizer.GetForCulture("Common.Save", "es-419"));
        Assert.Equal("Presupuestos", localizer.GetForCulture("Nav.Quotes", "es-419"));

        // Fallback para chave inexistente registra no tracker e retorna chave
        var missingResult = localizer.GetForCulture("Unregistered.Key.Test", "en-US");
        Assert.Equal("Unregistered.Key.Test", missingResult);
        var missingKeys = tracker.GetMissingKeys();
        Assert.Contains(missingKeys, m => m.Key == "Unregistered.Key.Test" && m.CultureCode == "en-US");
    }

    [Fact]
    public void DocumentSnapshotSerializer_Preserves_LanguageAndCurrencyCode()
    {
        var serializer = new DocumentSnapshotSerializer();

        var original = new DocumentSnapshot(
            new IssuerSnapshot("MNSOFT Soluções", "18.160.057/0001-13", "comercial@mnsoft.com.br", "9199999999", "Av. Paulista", "São Paulo", "SP", null, "pix@mnsoft.com.br", null),
            new CustomerSnapshot("Cliente Internacional", "PJ", "12.345.678/0001-90", null, "client@corp.com", null, "Miami", "FL"),
            new QuoteSnapshot("ORC-2026-MULTI", DateTime.UtcNow, DateTime.UtcNow.AddDays(15), "15 dias", "Pix", "À vista", "Observações", "essential", "#0284c7", "Obrigado", true, 2500m, 0m, 2500m, "en-US", "BRL"),
            [new QuoteItemSnapshot("Consultoria Técnica", "un", 1, 2500m, 0m, 2500m, 2500m)]
        );

        var serialized = serializer.Serialize(original);
        Assert.NotNull(serialized.Json);
        Assert.Contains("\"languageCode\":\"en-US\"", serialized.Json);
        Assert.Contains("\"currencyCode\":\"BRL\"", serialized.Json);

        // Deserializa e valida conformidade
        var deserialized = JsonSerializer.Deserialize<DocumentSnapshot>(serialized.Json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deserialized);
        Assert.Equal("en-US", deserialized.Quote.LanguageCode);
        Assert.Equal("BRL", deserialized.Quote.CurrencyCode);

        // Snapshot legado sem campos de locale deve atribuir defaults seguros
        var legacyJson = """
        {
          "issuer": { "name": "Empresa Legada" },
          "customer": { "name": "Cliente Antigo" },
          "quote": {
            "number": "ORC-LEGACY-01",
            "issueDate": "2026-01-01T00:00:00Z",
            "template": "essential",
            "showPlatformBrand": true,
            "subtotal": 100,
            "discount": 0,
            "total": 100
          },
          "items": []
        }
        """;
        var legacyDeserialized = JsonSerializer.Deserialize<DocumentSnapshot>(legacyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(legacyDeserialized);
        Assert.Equal("pt-BR", legacyDeserialized.Quote.LanguageCode);
        Assert.Equal("BRL", legacyDeserialized.Quote.CurrencyCode);
    }

    [Fact]
    public async Task QuestPdfGenerator_Multilingual_RendersValidPdfsInEnglishAndSpanish()
    {
        var generator = new QuestPdfDocumentService();
        var doc = new Document
        {
            Type = DocumentType.Budget,
            ClientName = "International Client Inc.",
            IssueDate = DateTime.UtcNow
        };
        doc.IssueNumber("ORC-EN-001");
        doc.Items.Add(new DocumentItem
        {
            Description = "Software Consulting",
            Quantity = 2,
            UnitPrice = 750m
        });
        doc.CalculateTotals();

        var issuer = new IssuerProfile
        {
            BusinessName = "Global Solutions",
            DocumentNumber = "12.345.678/0001-90",
            City = "São Paulo",
            Email = "contact@globalsolutions.com"
        };

        // Render in en-US
        var enBytes = await generator.GenerateDocumentPdfAsync(doc, issuer, PlanType.Professional, "en-US", "BRL");
        Assert.NotNull(enBytes);
        Assert.True(enBytes.Length > 100);
        Assert.Equal((byte)'%', enBytes[0]);
        Assert.Equal((byte)'P', enBytes[1]);
        Assert.Equal((byte)'D', enBytes[2]);
        Assert.Equal((byte)'F', enBytes[3]);

        // Render in es-ES
        var esBytes = await generator.GenerateDocumentPdfAsync(doc, issuer, PlanType.Professional, "es-ES", "BRL");
        Assert.NotNull(esBytes);
        Assert.True(esBytes.Length > 100);
        Assert.Equal((byte)'%', esBytes[0]);
        Assert.Equal((byte)'P', esBytes[1]);
        Assert.Equal((byte)'D', esBytes[2]);
        Assert.Equal((byte)'F', esBytes[3]);
    }

    [Fact]
    public async Task BudgetWizardService_SaveAsTemplate_CopiesItemsWithoutClientOrPayments()
    {
        var documents = new InMemoryRepo<Document>();
        var items = new InMemoryRepo<DocumentItem>();
        var templates = new InMemoryRepo<BudgetTemplate>();
        var templateItems = new InMemoryRepo<BudgetTemplateItem>();
        var services = new InMemoryRepo<ServiceCatalogItem>();
        var clients = new InMemoryRepo<Client>();
        var settings = new InMemoryRepo<AccountSettings>();

        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var document = new Document
        {
            AccountId = accountId,
            UserId = userId,
            Type = DocumentType.Budget,
            ClientId = clientId,
            ClientName = "Cliente Confidencial",
            Notes = "Condições gerais de prestação",
            PublicToken = "secret-token-12345"
        };
        document.IssueNumber("ORC-2026-TMPL");
        await documents.AddAsync(document);

        var docItem = new DocumentItem
        {
            DocumentId = document.Id,
            Description = "Instalação e Configuração de Servidor",
            Unit = "serviço",
            Quantity = 1,
            UnitPrice = 3500m,
            SortOrder = 1
        };
        await items.AddAsync(docItem);

        var wizard = new BudgetWizardService(
            documents, items, clients, services, templates, templateItems,
            settings, new FakeUow(), new FakeNumberService());

        var result = await wizard.SaveAsTemplateAsync(userId, accountId, document.Id, "Template Servidor Padrão", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value);

        var savedTemplate = templates.Items.FirstOrDefault(t => t.Id == result.Value);
        Assert.NotNull(savedTemplate);
        Assert.Equal("Template Servidor Padrão", savedTemplate.Title);
        Assert.Equal(accountId, savedTemplate.AccountId);
        Assert.False(savedTemplate.IsSystemTemplate);
        Assert.True(savedTemplate.IsActive);

        var savedItems = templateItems.Items.Where(ti => ti.BudgetTemplateId == savedTemplate.Id).ToList();
        Assert.Single(savedItems);
        Assert.Equal("Instalação e Configuração de Servidor", savedItems[0].Description);
        Assert.Equal(3500m, savedItems[0].UnitPrice);
        Assert.Equal(1, savedItems[0].Quantity);

        // Isolamento de dados: template gerado não carrega clientId nem tokens
        Assert.DoesNotContain("secret-token-12345", savedTemplate.Title);
        Assert.DoesNotContain("Cliente Confidencial", savedTemplate.Title);
    }

    private sealed class InMemoryRepo<T> : IRepository<T> where T : class
    {
        public List<T> Items { get; } = [];
        public Task<T?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult<T?>(null);
        public Task AddAsync(T entity, CancellationToken ct = default) { Items.Add(entity); return Task.CompletedTask; }
        public void Remove(T entity) => Items.Remove(entity);
        public IQueryable<T> Query() => Items.AsQueryable();
    }

    private sealed class FakeUow : IUnitOfWork
    {
        public bool HasActiveTransaction => false;
        public Task BeginTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task CommitTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeNumberService : IDocumentNumberService
    {
        public Task<string> NextAsync(Guid userId, DocumentType type, Guid? accountId = null, CancellationToken ct = default) =>
            Task.FromResult("ORC-999");
    }
}
