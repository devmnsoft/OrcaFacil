namespace OrcaFacil.Application.Commercial;

public sealed record CommercialBalance(
    Guid? DocumentId,
    Guid? WorkOrderId,
    string ContractSource,
    decimal ContractedAmount,
    decimal ReceivedAmount,
    decimal ReversedAmount,
    decimal BalanceAmount,
    decimal OverpaidAmount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string>? BlockingWarnings = null)
{
    public bool IsSettled => BalanceAmount == 0m && OverpaidAmount == 0m && !HasBlockingDivergence;
    public bool HasOverpayment => OverpaidAmount > 0m;
    public bool HasBlockingDivergence => BlockingWarnings is { Count: > 0 };
}

public interface ICommercialBalanceService
{
    Task<CommercialBalance?> GetForWorkOrderAsync(Guid accountId, Guid workOrderId, CancellationToken ct = default);
    Task<CommercialBalance?> GetForDocumentAsync(Guid accountId, Guid documentId, Guid? preferredWorkOrderId = null, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, CommercialBalance>> GetForDocumentsAsync(Guid accountId, IReadOnlyCollection<Guid> documentIds,
        IReadOnlyDictionary<Guid, Guid>? preferredWorkOrderIds = null, CancellationToken ct = default);
}

public sealed record CommercialPaymentAmount(decimal Amount, bool IsReversed);

public enum CommercialPaymentClassification
{
    IncludedInSelectedContract,
    OtherOrigin,
    ExcludedInconsistent,
    Reversed
}

public sealed record CommercialPaymentClassificationResult(CommercialPaymentClassification Classification,
    string Code, string Label, string Reason)
{
    public bool ParticipatesInBalance => Classification == CommercialPaymentClassification.IncludedInSelectedContract;
    public bool IsExcluded => Classification == CommercialPaymentClassification.ExcludedInconsistent;
    public bool IsReversed => Classification == CommercialPaymentClassification.Reversed;
}

public static class CommercialPaymentClassifier
{
    public static CommercialPaymentClassificationResult Classify(Guid? paymentDocumentId, Guid? paymentWorkOrderId,
        bool isReversed, Guid documentId, Guid? selectedWorkOrderId)
    {
        if (isReversed)
            return new(CommercialPaymentClassification.Reversed, "reversed", "Estornado",
                "Pagamento preservado no histórico, mas estornado; não reduz o saldo vigente.");

        if (HasIncompatibleDocumentLink(paymentDocumentId, paymentWorkOrderId, documentId, selectedWorkOrderId))
            return new(CommercialPaymentClassification.ExcludedInconsistent, "excluded-inconsistent",
                "Excluído por inconsistência",
                "Vínculo incompatível: o pagamento aponta para a ordem selecionada, mas pertence a outro orçamento.");

        if (selectedWorkOrderId.HasValue)
        {
            if (paymentWorkOrderId == selectedWorkOrderId.Value)
                return new(CommercialPaymentClassification.IncludedInSelectedContract, "included",
                    "Incluído no contrato selecionado",
                    "Recebimento vinculado à ordem de serviço selecionada como origem contratual.");

            return new(CommercialPaymentClassification.OtherOrigin, "other-origin", "Pertencente a outra origem",
                paymentWorkOrderId.HasValue
                    ? "Recebimento vinculado a outra ordem de serviço; não compõe este saldo."
                    : "Recebimento direto do orçamento; a consulta da ordem selecionada não transfere valores entre origens.");
        }

        if (paymentDocumentId == documentId && paymentWorkOrderId is null)
            return new(CommercialPaymentClassification.IncludedInSelectedContract, "included",
                "Incluído no contrato selecionado",
                "Recebimento direto vinculado ao orçamento selecionado.");

        return new(CommercialPaymentClassification.OtherOrigin, "other-origin", "Pertencente a outra origem",
            "Recebimento vinculado a outra origem; não compõe este saldo.");
    }

    public static bool HasIncompatibleDocumentLink(Guid? paymentDocumentId, Guid? paymentWorkOrderId,
        Guid? documentId, Guid? selectedWorkOrderId) =>
        documentId.HasValue &&
        selectedWorkOrderId.HasValue &&
        paymentWorkOrderId == selectedWorkOrderId.Value &&
        paymentDocumentId is { } linkedDocumentId &&
        linkedDocumentId != documentId.Value;

    public static bool IsSelectedOrigin(Guid? paymentDocumentId, Guid? paymentWorkOrderId,
        Guid documentId, Guid? selectedWorkOrderId) =>
        !HasIncompatibleDocumentLink(paymentDocumentId, paymentWorkOrderId, documentId, selectedWorkOrderId) &&
        (selectedWorkOrderId.HasValue
            ? paymentWorkOrderId == selectedWorkOrderId.Value
            : paymentDocumentId == documentId && paymentWorkOrderId is null);
}

public static class CommercialBalanceCalculator
{
    public static CommercialBalance Calculate(Guid? documentId, Guid? workOrderId, string source, decimal contractedAmount,
        IEnumerable<CommercialPaymentAmount> payments, IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? blockingWarnings = null)
    {
        var paymentList = payments.ToArray();
        var active = paymentList.Where(x => !x.IsReversed).Sum(x => x.Amount);
        var reversed = paymentList.Where(x => x.IsReversed).Sum(x => x.Amount);
        var balance = CommercialCalculator.Round(contractedAmount - active);
        var overpaid = balance < 0m ? Math.Abs(balance) : 0m;

        return new CommercialBalance(
            documentId,
            workOrderId,
            source,
            CommercialCalculator.Round(contractedAmount),
            CommercialCalculator.Round(active),
            CommercialCalculator.Round(reversed),
            balance > 0m ? balance : 0m,
            CommercialCalculator.Round(overpaid),
            warnings ?? [],
            blockingWarnings ?? []);
    }
}
