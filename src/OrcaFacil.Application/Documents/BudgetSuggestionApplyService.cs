using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Ai;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Documents;

public sealed record BudgetSuggestionApplyItem(Guid CatalogItemId, decimal? Quantity);

public sealed record BudgetSuggestionApplyResult(bool Succeeded, string? Error, Guid? DocumentId, bool Conflict = false)
{
    public static BudgetSuggestionApplyResult Ok(Guid documentId) => new(true, null, documentId);
    public static BudgetSuggestionApplyResult Fail(string error, bool conflict = false) => new(false, error, null, conflict);
}

/// <summary>
/// Comando servidor que aplica uma revisão de sugestão de IA a um rascunho de orçamento
/// de forma atômica e idempotente:
/// documento e transição da revisão são gravados na mesma transação e a transição
/// usa compare-and-set no banco (status PendingReview), o que impede dois rascunhos
/// em cliques simultâneos. A repetição com a mesma seleção retorna o mesmo documento;
/// a repetição com seleção diferente gera conflito.
/// </summary>
public sealed class BudgetSuggestionApplyService(
    IAiSuggestionReviewService reviews,
    BudgetWizardService wizard,
    IRepository<Document> documents,
    IUnitOfWork unitOfWork)
{
    public const int MaxSelectedItems = 30;

    public async Task<BudgetSuggestionApplyResult> ApplyAsync(Guid userId, Guid accountId, Guid reviewId,
        IReadOnlyList<BudgetSuggestionApplyItem> selection, CancellationToken ct)
    {
        var review = await reviews.FindAsync(accountId, reviewId, ct);
        if (review is null)
            return BudgetSuggestionApplyResult.Fail("A revisão não foi encontrada nesta conta.");

        var normalized = Normalize(review, selection, out var validationError);
        if (validationError is not null)
            return BudgetSuggestionApplyResult.Fail(validationError);

        var fingerprint = ComputeFingerprint(reviewId, normalized);

        if (string.Equals(review.Status, "Applied", StringComparison.OrdinalIgnoreCase))
        {
            if (review.AppliedDocumentId is Guid appliedId
                && string.Equals(review.ApplyFingerprint, fingerprint, StringComparison.Ordinal)
                && DocumentExists(accountId, appliedId))
                return BudgetSuggestionApplyResult.Ok(appliedId);
            return BudgetSuggestionApplyResult.Fail(
                "Esta revisão já foi aplicada com outra seleção ou conteúdo. Gere uma nova sugestão para aplicar um conteúdo diferente.",
                conflict: true);
        }
        if (!string.Equals(review.Status, "PendingReview", StringComparison.OrdinalIgnoreCase))
            return BudgetSuggestionApplyResult.Fail("Esta revisão não está mais pendente de aplicação.");

        var priceError = RevalidateServicesAndPrices(accountId, review, normalized, out var seeds);
        if (priceError is not null)
            return BudgetSuggestionApplyResult.Fail(priceError, conflict: true);

        var suggestionText = BuildSuggestionText(review);
        var idempotencyKey = "ai-apply:" + reviewId.ToString("N");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var created = await wizard.CreateSuggestionDraftAsync(userId, accountId, seeds, suggestionText, idempotencyKey, ct);
            if (!created.Succeeded || created.Draft is null)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return BudgetSuggestionApplyResult.Fail(created.Error ?? "Não foi possível criar o rascunho.", created.Conflict);
            }

            var claimed = await reviews.TryMarkAppliedAsync(accountId, reviewId, fingerprint, created.Draft.DocumentId, ct);
            if (!claimed)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return await ResolveAfterLostClaimAsync(accountId, reviewId, fingerprint, ct);
            }

            await unitOfWork.CommitTransactionAsync(ct);
            return BudgetSuggestionApplyResult.Ok(created.Draft.DocumentId);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private async Task<BudgetSuggestionApplyResult> ResolveAfterLostClaimAsync(Guid accountId, Guid reviewId, string fingerprint, CancellationToken ct)
    {
        var current = await reviews.FindAsync(accountId, reviewId, ct);
        if (current is not null
            && string.Equals(current.Status, "Applied", StringComparison.OrdinalIgnoreCase)
            && string.Equals(current.ApplyFingerprint, fingerprint, StringComparison.Ordinal)
            && current.AppliedDocumentId is Guid documentId
            && DocumentExists(accountId, documentId))
            return BudgetSuggestionApplyResult.Ok(documentId);
        return BudgetSuggestionApplyResult.Fail(
            "Esta revisão foi aplicada em outra janela com uma seleção diferente. Gere uma nova sugestão para aplicar um conteúdo diferente.",
            conflict: true);
    }

    private bool DocumentExists(Guid accountId, Guid documentId) =>
        documents.Query().Any(x => x.Id == documentId && x.AccountId == accountId && !x.IsDeleted);

    private string? RevalidateServicesAndPrices(Guid accountId, AiBudgetSuggestionReview review,
        IReadOnlyList<BudgetSuggestionApplyItem> normalized, out List<BudgetWizardService.DraftServiceSeed> seeds)
    {
        seeds = [];
        var reviewById = review.Items.ToDictionary(x => x.CatalogItemId);
        var changedPrices = new List<string>();
        foreach (var item in normalized)
        {
            var service = wizard.FindAccountService(accountId, item.CatalogItemId);
            if (service is null)
                return $"O serviço \"{reviewById[item.CatalogItemId].Description}\" não está mais ativo no catálogo desta conta. Gere uma nova sugestão.";
            var current = CommercialCalculator.Round(service.StandardPrice);
            var suggested = CommercialCalculator.Round(reviewById[item.CatalogItemId].UnitPrice);
            if (current != suggested)
                changedPrices.Add($"\"{service.Name}\" (de {suggested.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))} para {current.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))})");
            seeds.Add(new(service, item.Quantity!.Value));
        }
        if (changedPrices.Count > 0)
            return "O preço de catálogo mudou após a sugestão: " + string.Join(", ", changedPrices) +
                   ". Nenhum valor foi trocado silenciosamente. Gere uma nova sugestão para revisar os preços atuais.";
        return null;
    }

    private static IReadOnlyList<BudgetSuggestionApplyItem> Normalize(AiBudgetSuggestionReview review,
        IReadOnlyList<BudgetSuggestionApplyItem> selection, out string? error)
    {
        error = null;
        var allowed = review.Items.ToDictionary(x => x.CatalogItemId);
        var normalized = new List<BudgetSuggestionApplyItem>();
        foreach (var item in selection)
        {
            if (item.CatalogItemId == Guid.Empty || !allowed.TryGetValue(item.CatalogItemId, out var suggested))
                continue;
            // Quantidade ausente (campo não informado) preserva a quantidade aprovada na revisão.
            var quantity = item.Quantity ?? suggested.Quantity;
            if (quantity <= 0 || quantity > 10000)
            {
                error = "A quantidade de um item selecionado é inválida.";
                return [];
            }
            if (normalized.Any(x => x.CatalogItemId == item.CatalogItemId))
                continue;
            normalized.Add(new(item.CatalogItemId, quantity));
        }
        if (normalized.Count == 0)
        {
            error = "Selecione ao menos um item do catálogo antes de criar o rascunho.";
            return [];
        }
        if (normalized.Count > MaxSelectedItems)
        {
            error = $"A aplicação aceita no máximo {MaxSelectedItems} itens.";
            return [];
        }
        return normalized;
    }

    private static string ComputeFingerprint(Guid reviewId, IReadOnlyList<BudgetSuggestionApplyItem> items)
    {
        var payload = reviewId.ToString("N") + "|" + string.Join("|", items
            .OrderBy(x => x.CatalogItemId)
            .Select(x => x.CatalogItemId.ToString("N") + ":" + x.Quantity!.Value.ToString("0.####", CultureInfo.InvariantCulture)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string? BuildSuggestionText(AiBudgetSuggestionReview review)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(review.Scope)) parts.Add("Escopo revisado: " + review.Scope.Trim());
        if (!string.IsNullOrWhiteSpace(review.Notes)) parts.Add("Observações: " + review.Notes.Trim());
        return parts.Count == 0 ? null : string.Join("\n", parts);
    }
}
