using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Ai;

public interface IAiOrchestrator
{
    Task<AiExecutionResult> ExecuteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string purpose,
        AiClientRequest request,
        string? preferredProvider = null,
        CancellationToken ct = default);
}

public sealed record AiExecutionResult(
    bool Succeeded,
    string Content,
    AiOperatingMode Mode,
    string Provider,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    long LatencyMs,
    bool IsFallbackToRules,
    string? Message = null);

public sealed class AiCircuitBreaker
{
    private readonly ConcurrentDictionary<string, CircuitState> _states = new(StringComparer.OrdinalIgnoreCase);

    public bool CanAttempt(string provider)
    {
        if (!_states.TryGetValue(provider, out var state)) return true;
        if (state.IsOpen && DateTime.UtcNow > state.OpenUntil)
        {
            state.IsOpen = false;
            state.Failures = 0;
            return true;
        }
        return !state.IsOpen;
    }

    public void RecordSuccess(string provider)
    {
        var state = _states.GetOrAdd(provider, _ => new CircuitState());
        state.Failures = 0;
        state.IsOpen = false;
    }

    public void RecordFailure(string provider)
    {
        var state = _states.GetOrAdd(provider, _ => new CircuitState());
        state.Failures++;
        if (state.Failures >= 3)
        {
            state.IsOpen = true;
            state.OpenUntil = DateTime.UtcNow.AddSeconds(60);
        }
    }

    private sealed class CircuitState
    {
        public int Failures { get; set; }
        public bool IsOpen { get; set; }
        public DateTime OpenUntil { get; set; }
    }
}

// -------------------------------------------------------------
// FEATURE 1: Assistente de Orçamento
// -------------------------------------------------------------
public sealed record BudgetAiItemSuggestion(
    Guid? CatalogItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string UnitCode,
    bool MatchedFromCatalog);

public sealed record BudgetAiSuggestionResult(
    bool Succeeded,
    string SuggestedScope,
    string SuggestedNotes,
    IReadOnlyList<BudgetAiItemSuggestion> Items,
    decimal EstimatedTotal,
    bool IsRuleBased,
    string Notice);

public interface IBudgetAiAssistant
{
    Task<BudgetAiSuggestionResult> SuggestBudgetAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string serviceDescription,
        IReadOnlyList<ServiceCatalogItem> availableCatalog,
        CancellationToken ct = default);
}

// -------------------------------------------------------------
// FEATURE 2: Revisão Comercial
// -------------------------------------------------------------
public sealed record CommercialReviewFinding(
    string Category,
    string Severity,
    string Message,
    string? Suggestion);

public sealed record CommercialReviewResult(
    bool Succeeded,
    IReadOnlyList<CommercialReviewFinding> Findings,
    string? SuggestedTitle,
    string? SuggestedConditions,
    bool IsRuleBased);

public interface ICommercialAiReviewer
{
    Task<CommercialReviewResult> ReviewQuoteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        Document document,
        IReadOnlyList<DocumentItem> items,
        Client? client,
        CancellationToken ct = default);
}

// -------------------------------------------------------------
// FEATURE 3: Rascunho de Mensagem
// -------------------------------------------------------------
public sealed record MessageDraftResult(
    string Channel,
    string MessageType,
    string DraftContent,
    bool Sent,
    IReadOnlyList<string> Disclaimers);

public interface IMessageDraftAiAssistant
{
    MessageDraftResult GenerateDraft(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string channel,
        string messageType,
        string customerName,
        string documentNumber,
        decimal totalAmount,
        DateTime? expirationDate);
}

// -------------------------------------------------------------
// FEATURE 4: Ajuda Contextual
// -------------------------------------------------------------
public interface IContextualAiHelpService
{
    Task<AiAnswer> AskHelpAsync(
        AiRequestContext context,
        string question,
        IEnumerable<AiSource> knowledgeBase,
        CancellationToken ct = default);
}

// =============================================================
// IMPLEMENTAÇÕES
// =============================================================

public sealed class BudgetAiAssistant(
    IAiOrchestrator orchestrator,
    PromptSanitizer sanitizer) : IBudgetAiAssistant
{
    public async Task<BudgetAiSuggestionResult> SuggestBudgetAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string serviceDescription,
        IReadOnlyList<ServiceCatalogItem> availableCatalog,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(serviceDescription))
            return new(false, string.Empty, string.Empty, [], 0, true, "Informe a descrição do serviço para receber sugestões.");

        var cleanDescription = sanitizer.Sanitize(serviceDescription);
        var activeCatalog = availableCatalog
            .Where(x => x.AccountId == context.AccountId && x.IsActive && !x.IsDeleted)
            .ToList();

        // 1. Keyword-based matching from tenant catalog (deterministic baseline)
        var matchedItems = new List<BudgetAiItemSuggestion>();
        var tokens = cleanDescription.ToLowerInvariant().Split([' ', ',', ';', '.', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var item in activeCatalog)
        {
            var itemNameLower = item.Name.ToLowerInvariant();
            if (tokens.Any(t => t.Length > 2 && itemNameLower.Contains(t)))
            {
                matchedItems.Add(new BudgetAiItemSuggestion(
                    item.Id,
                    item.Name,
                    1m,
                    item.StandardPrice,
                    item.StandardPrice,
                    item.UnitCode ?? "UN",
                    true));
            }
        }

        // Limit to 5 suggestions
        if (matchedItems.Count > 5)
            matchedItems = matchedItems.Take(5).ToList();

        // Try LLM refinement if available
        var clientRequest = new AiClientRequest(
            Prompt: $"Descrição do serviço solicitado pelo cliente:\n{cleanDescription}\n\nCatálogo disponível:\n" +
                    string.Join("\n", activeCatalog.Select(x => $"- {x.Name} (Preço tabela: R$ {x.StandardPrice:F2}, Un: {x.UnitCode})")),
            SystemPrompt: "Você é um assistente de orçamentos para profissionais e prestadores. " +
                          "Seu papel é estruturar escopo e observações técnicas a partir da descrição. " +
                          "NÃO invente valores diferentes do catálogo. Retorne apenas o escopo sugerido e observações recomendadas.");

        var execution = await orchestrator.ExecuteAsync(context, policy, "budget_assistant", clientRequest, null, ct);

        string scope;
        string notes;

        if (execution.Succeeded && !execution.IsFallbackToRules)
        {
            scope = execution.Content;
            notes = "Orçamento gerado com base no catálogo oficial do prestador. Válido por 10 dias.";
        }
        else
        {
            scope = $"Execução dos serviços conforme descrição informada: {cleanDescription}.";
            notes = "Condições gerais: Execução conforme especificações combinadas. Pagamento na entrega ou conforme combinado.";
        }

        var total = matchedItems.Sum(x => x.TotalPrice);
        return new BudgetAiSuggestionResult(
            true,
            scope,
            notes,
            matchedItems,
            total,
            execution.IsFallbackToRules,
            execution.IsFallbackToRules
                ? "Sugestão gerada por regras internas baseada no seu catálogo de serviços."
                : "Sugestão auxiliada por IA com preços fixados no seu catálogo. Revise antes de aplicar.");
    }
}

public sealed class CommercialAiReviewer : ICommercialAiReviewer
{
    public Task<CommercialReviewResult> ReviewQuoteAsync(
        AiRequestContext context,
        AiGovernancePolicy policy,
        Document document,
        IReadOnlyList<DocumentItem> items,
        Client? client,
        CancellationToken ct = default)
    {
        var findings = new List<CommercialReviewFinding>();

        if (string.IsNullOrWhiteSpace(document.ClientName) || document.ClientName.Trim().Length < 2)
        {
            findings.Add(new("Geral", "Média", "Cliente do orçamento não identificado claramente.",
                "Identifique o nome do cliente na proposta para formalizar o documento."));
        }

        if (client is null)
        {
            findings.Add(new("Cliente", "Alta", "Nenhum cliente está associado ao documento.",
                "Vincule um cliente para possibilitar aprovação, envio e emissão de recibo."));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(client.Email) && string.IsNullOrWhiteSpace(client.Phone))
            {
                findings.Add(new("Cliente", "Média", "Cliente sem telefone e sem e-mail cadastrados.",
                    "Cadastre ao menos um canal de contato para facilitar o compartilhamento da proposta."));
            }
        }

        if (items.Count == 0)
        {
            findings.Add(new("Itens", "Crítica", "O orçamento não possui nenhum item cadastrado.",
                "Inclua ao menos um serviço ou produto para compor o valor."));
        }
        else
        {
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    findings.Add(new("Itens", "Alta", "Há item sem descrição detalhada.",
                        "Especifique com clareza o que está incluso no item."));
                }
                if (item.UnitPrice <= 0)
                {
                    findings.Add(new("Valores", "Alta", $"O item '{item.Description}' possui valor unitário zerado.",
                        "Defina o valor correto para evitar propostas inconsistentes."));
                }
            }
        }

        if (!document.ValidUntil.HasValue || document.ValidUntil.Value < DateTime.UtcNow)
        {
            findings.Add(new("Condições", "Baixa", "Não há prazo de validade futuro definido para a proposta.",
                "Defina uma validade (ex.: 15 dias) para proteger seus custos contra variações de mercado."));
        }

        var suggestedTitle = !string.IsNullOrWhiteSpace(document.ClientName)
            ? $"Proposta Comercial - {document.ClientName}"
            : (client != null ? $"Proposta Comercial - {client.Name}" : "Orçamento de Serviços");

        var suggestedConditions = "Proposta válida por 15 dias. Início dos serviços mediante aprovação. " +
                                  "Garantia de 90 dias sobre a mão de obra.";

        return Task.FromResult(new CommercialReviewResult(
            true,
            findings,
            suggestedTitle,
            suggestedConditions,
            true));
    }
}

public sealed class MessageDraftAiAssistant(IAiRedactionService redaction) : IMessageDraftAiAssistant
{
    public MessageDraftResult GenerateDraft(
        AiRequestContext context,
        AiGovernancePolicy policy,
        string channel,
        string messageType,
        string customerName,
        string documentNumber,
        decimal totalAmount,
        DateTime? expirationDate)
    {
        var cleanCustomer = redaction.Sanitize(customerName);
        var cleanNumber = redaction.Sanitize(documentNumber);

        string content;
        if (channel.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase))
        {
            if (messageType.Equals("Presentation", StringComparison.OrdinalIgnoreCase))
            {
                content = $"Olá, {cleanCustomer}! Tudo bem?\n\n" +
                          $"Segue a proposta comercial *{cleanNumber}* com o valor total de *{totalAmount:C2}*.\n" +
                          (expirationDate.HasValue ? $"A proposta é válida até {expirationDate.Value:dd/MM/yyyy}.\n\n" : "\n") +
                          "Você pode visualizar todos os detalhes e aprovar pelo link seguro que enviamos. Qualquer dúvida estou à disposição!";
            }
            else
            {
                content = $"Olá, {cleanCustomer}! Tudo bem?\n\n" +
                          $"Gostaria de saber se você teve oportunidade de avaliar a proposta *{cleanNumber}*.\n" +
                          "Se tiver qualquer dúvida ou quiser ajustar algum item, estou à disposição para conversarmos!";
            }
        }
        else
        {
            if (messageType.Equals("Presentation", StringComparison.OrdinalIgnoreCase))
            {
                content = $"Prezado(a) {cleanCustomer},\n\n" +
                          $"Encaminhamos a proposta comercial {cleanNumber} no valor de {totalAmount:C2}.\n" +
                          (expirationDate.HasValue ? $"Condições válidas até {expirationDate.Value:dd/MM/yyyy}.\n\n" : "\n") +
                          "Ficamos à disposição para quaisquer esclarecimentos técnicos ou comerciais.\n\nAtenciosamente,";
            }
            else
            {
                content = $"Prezado(a) {cleanCustomer},\n\n" +
                          $"Esperamos que esteja bem. Gostaríamos de verificar se teve a oportunidade de analisar a proposta comercial {cleanNumber}.\n" +
                          "Estamos à disposição para alinhar dúvidas ou fazer qualquer adequação necessária.\n\nAtenciosamente,";
            }
        }

        return new MessageDraftResult(
            channel,
            messageType,
            content,
            false,
            ["Este rascunho é uma sugestão de texto e NUNCA é enviado automaticamente.",
             "Revise e personalize a mensagem antes de copiar e enviar pelo seu aplicativo."]);
    }
}

public sealed class ContextualAiHelpService(
    AiPromptInjectionGuard injectionGuard,
    IAiOrchestrator orchestrator) : IContextualAiHelpService
{
    public async Task<AiAnswer> AskHelpAsync(
        AiRequestContext context,
        string question,
        IEnumerable<AiSource> knowledgeBase,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            return new("Digite sua dúvida para buscar ajuda.", AiOperatingMode.RulesOnly, AiConfidence.Insufficient, [], []);

        // Pre-filter sources BEFORE calling LLM
        var candidateSources = knowledgeBase
            .Where(x => x.AccountId == context.AccountId && x.IsAccessible && !injectionGuard.IsSuspicious(x.Content))
            .ToList();

        var qLower = question.ToLowerInvariant();
        var relevant = candidateSources
            .Where(x => x.Title.ToLowerInvariant().Contains(qLower) ||
                        x.Content.ToLowerInvariant().Contains(qLower) ||
                        qLower.Split(' ').Any(w => w.Length > 3 && x.Content.ToLowerInvariant().Contains(w)))
            .Take(3)
            .ToList();

        if (relevant.Count == 0)
        {
            return new(
                "A base autorizada não possui informação suficiente para responder a esta dúvida.",
                AiOperatingMode.SecureRag,
                AiConfidence.Insufficient,
                [],
                ["Nenhuma fonte acessível e documentada foi encontrada para o termo pesquisado."]);
        }

        var sourceContext = string.Join("\n\n", relevant.Select(r => $"Fonte: {r.Title}\n{r.Content}"));
        var clientRequest = new AiClientRequest(
            Prompt: $"Pergunta do usuário: {question}\n\nFontes oficiais:\n{sourceContext}",
            SystemPrompt: "Responda à dúvida do usuário exclusivamente com base nas fontes oficiais fornecidas. " +
                          "Se a fonte não tiver a resposta, declare que não possui evidência. Não invente.");

        var execution = await orchestrator.ExecuteAsync(
            context,
            new AiGovernancePolicy(context.AccountId),
            "contextual_help",
            clientRequest,
            null,
            ct);

        if (execution.Succeeded && !execution.IsFallbackToRules)
        {
            return new(
                execution.Content,
                AiOperatingMode.SecureRag,
                relevant.Count > 1 ? AiConfidence.High : AiConfidence.Medium,
                relevant,
                ["Resposta gerada com base estrita na documentação autorizada do produto."],
                "Consulte os artigos relacionados se precisar de mais detalhes.");
        }

        // Rule-based fallback summary of sources
        var ruleAnswer = string.Join("\n\n", relevant.Select(x => $"**{x.Title}**:\n{x.Content}"));
        return new(
            ruleAnswer,
            AiOperatingMode.RulesOnly,
            AiConfidence.Medium,
            relevant,
            ["Resposta baseada diretamente no texto das fontes oficiais."],
            "Revise as fontes documentadas.");
    }
}
