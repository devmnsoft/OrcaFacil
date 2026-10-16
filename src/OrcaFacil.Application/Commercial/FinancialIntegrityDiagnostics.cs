namespace OrcaFacil.Application.Commercial;

/// <summary>
/// Non-destructive diagnostic of historical financial links (quote, work order, payment and receipt).
/// The analyzer is pure so it can be exercised without a database; persistence loads the rows.
/// It never mutates anything: it only reports divergences with a deterministic technical id, origin,
/// risk and recommended corrective action so a SuperAdministrator can review each occurrence.
/// </summary>
public enum FinancialDivergenceSeverity
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3
}

public sealed record FinancialPaymentRow(
    Guid Id,
    Guid AccountId,
    Guid ClientId,
    decimal Amount,
    bool IsReversed,
    Guid? DocumentId,
    Guid? WorkOrderId,
    DateTime PaidAt);

public sealed record FinancialDocumentRow(
    Guid Id,
    Guid AccountId,
    Guid? ClientId,
    string Number,
    decimal Total,
    bool IsDeleted);

public sealed record FinancialWorkOrderRow(
    Guid Id,
    Guid AccountId,
    Guid ClientId,
    Guid? SourceDocumentId,
    string Number,
    bool IsCancelled,
    bool IsDeleted);

public sealed record FinancialReceiptRow(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    Guid PaymentId,
    string Number,
    bool IsCancelled,
    bool IsDeleted);

public sealed record FinancialIntegrityFinding(
    string FindingId,
    Guid OccurrenceId,
    Guid AccountId,
    string AccountName,
    string TypeCode,
    string TypeLabel,
    FinancialDivergenceSeverity Severity,
    string Origin,
    decimal Amount,
    string Risk,
    string RecommendedAction);

public sealed record FinancialIntegrityReport(
    IReadOnlyList<FinancialIntegrityFinding> Findings,
    int ScannedAccounts,
    DateTime GeneratedAt,
    bool Truncated = false)
{
    public bool HasFindings => Findings.Count > 0;
    public int CriticalCount => Findings.Count(x => x.Severity == FinancialDivergenceSeverity.Critical);
    public int HighCount => Findings.Count(x => x.Severity == FinancialDivergenceSeverity.High);
    public int MediumCount => Findings.Count(x => x.Severity == FinancialDivergenceSeverity.Medium);
    public int LowCount => Findings.Count(x => x.Severity == FinancialDivergenceSeverity.Low);
    public decimal AffectedAmount => Findings.Sum(x => x.Amount);
}

public interface IFinancialIntegrityDiagnosticService
{
    /// <summary>Scans one tenant. Bounded by the number of records of that account.</summary>
    Task<FinancialIntegrityReport> ScanAccountAsync(Guid accountId, CancellationToken ct = default);

    /// <summary>Scans every tenant, capped by <paramref name="maxPayments"/> to protect the host.</summary>
    Task<FinancialIntegrityReport> ScanAllAsync(int maxPayments = 5000, CancellationToken ct = default);
}

public static class FinancialIntegrityAnalyzer
{
    private const int ShortIdLength = 8;

    public static IReadOnlyList<FinancialIntegrityFinding> Analyze(
        IReadOnlyCollection<FinancialPaymentRow> payments,
        IReadOnlyCollection<FinancialDocumentRow> documents,
        IReadOnlyCollection<FinancialWorkOrderRow> workOrders,
        IReadOnlyCollection<FinancialReceiptRow> receipts,
        IReadOnlyDictionary<Guid, string>? accountNames = null)
    {
        var documentById = documents.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
        var workOrderById = workOrders.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
        var paymentById = payments.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
        var findings = new List<FinancialIntegrityFinding>();

        foreach (var payment in payments)
        {
            var accountName = ResolveAccountName(accountNames, payment.AccountId);
            var document = payment.DocumentId is { } documentId && documentById.TryGetValue(documentId, out var foundDocument)
                ? foundDocument
                : null;
            var workOrder = payment.WorkOrderId is { } workOrderId && workOrderById.TryGetValue(workOrderId, out var foundWorkOrder)
                ? foundWorkOrder
                : null;
            var origin = ResolveOrigin(document, workOrder, payment.Id);

            if (payment.DocumentId is null && payment.WorkOrderId is null)
            {
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-without-origin",
                    "Recebimento sem origem comercial", FinancialDivergenceSeverity.Critical,
                    origin, payment.Amount,
                    "O recebimento não pode ser conciliado com orçamento nem com ordem de serviço, distorcendo saldos e relatórios.",
                    "Localizar o orçamento ou a ordem de origem com o cliente e vincular manualmente após revisão.");
                continue;
            }

            if (workOrder is not null && workOrder.AccountId != payment.AccountId)
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-cross-account-link",
                    "Recebimento vinculado a ordem de outra conta", FinancialDivergenceSeverity.Critical,
                    origin, payment.Amount,
                    "O recebimento referencia uma ordem de serviço pertencente a outra conta, violando o isolamento multi-tenant.",
                    "Isolar os registros imediatamente e revisar a origem do lançamento antes de qualquer conciliação.");

            if (document is not null && document.AccountId != Guid.Empty && document.AccountId != payment.AccountId)
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-cross-account-link",
                    "Recebimento vinculado a orçamento de outra conta", FinancialDivergenceSeverity.Critical,
                    origin, payment.Amount,
                    "O recebimento referencia um orçamento pertencente a outra conta, violando o isolamento multi-tenant.",
                    "Isolar os registros imediatamente e revisar a origem do lançamento antes de qualquer conciliação.");

            if (payment.DocumentId is not null && (document is null || document.IsDeleted))
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-missing-document",
                    "Recebimento aponta para orçamento inexistente ou excluído", FinancialDivergenceSeverity.High,
                    origin, payment.Amount,
                    "O orçamento de origem não está mais disponível; o vínculo financeiro ficou órfão.",
                    "Reatribuir o recebimento ao orçamento vigente correto, preservando o histórico do lançamento.");

            if (payment.WorkOrderId is not null && (workOrder is null || workOrder.IsDeleted))
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-missing-workorder",
                    "Recebimento aponta para ordem de serviço inexistente ou excluída", FinancialDivergenceSeverity.High,
                    origin, payment.Amount,
                    "A ordem de serviço vinculada não está disponível; o saldo da ordem não é confiável.",
                    "Revisar a ordem de origem e revincular o recebimento, registrando a correção na auditoria.");

            if (workOrder is not null && !workOrder.IsDeleted)
            {
                if (workOrder.SourceDocumentId is { } sourceDocumentId &&
                    payment.DocumentId is { } linkedDocumentId &&
                    sourceDocumentId != linkedDocumentId)
                {
                    Add(findings, payment.AccountId, payment.Id, accountName, "payment-incompatible-link",
                        "Vínculo incompatível entre orçamento e ordem", FinancialDivergenceSeverity.Critical,
                        origin, payment.Amount,
                        "O recebimento aponta para a ordem selecionada, mas pertence a outro orçamento. Ele é excluído do saldo e bloqueia novos recebimentos.",
                        "Confirmar com o cliente qual contrato é o correto e ajustar o vínculo em revisão autorizada.");
                }
                else if (workOrder.ClientId != payment.ClientId)
                {
                    Add(findings, payment.AccountId, payment.Id, accountName, "payment-client-mismatch",
                        "Recebimento pertence a outro cliente", FinancialDivergenceSeverity.High,
                        origin, payment.Amount,
                        "O cliente do recebimento difere do cliente da ordem de serviço, quebrando o isolamento comercial.",
                        "Conferir o cliente pagador e corrigir o vínculo do recebimento com revisão registrada.");
                }
            }
            else if (document is not null && !document.IsDeleted && document.ClientId is { } documentClientId &&
                     documentClientId != payment.ClientId)
            {
                Add(findings, payment.AccountId, payment.Id, accountName, "payment-client-mismatch",
                    "Recebimento pertence a outro cliente", FinancialDivergenceSeverity.High,
                    origin, payment.Amount,
                    "O cliente do recebimento difere do cliente do orçamento, quebrando o isolamento comercial.",
                    "Conferir o cliente pagador e corrigir o vínculo do recebimento com revisão registrada.");
            }

            if (payment.IsReversed &&
                workOrder is not null && workOrder.SourceDocumentId is { } reversedSource &&
                payment.DocumentId is { } reversedDocumentId && reversedSource != reversedDocumentId)
            {
                Add(findings, payment.AccountId, payment.Id, accountName, "reversed-payment-inconsistent-link",
                    "Estorno com vínculo histórico inconsistente", FinancialDivergenceSeverity.Medium,
                    origin, payment.Amount,
                    "O estorno não altera o saldo vigente, mas o vínculo histórico permanece ambíguo para auditoria.",
                    "Manter o registro preservado e anotar a origem correta na revisão do histórico.");
            }
        }

        foreach (var workOrder in workOrders)
        {
            if (workOrder.IsDeleted || workOrder.SourceDocumentId is not { } sourceDocumentId) continue;
            var accountName = ResolveAccountName(accountNames, workOrder.AccountId);

            if (!documentById.TryGetValue(sourceDocumentId, out var sourceDocument) || sourceDocument.IsDeleted)
            {
                Add(findings, workOrder.AccountId, workOrder.Id, accountName, "workorder-missing-document",
                    "Ordem de serviço aponta para orçamento inexistente", FinancialDivergenceSeverity.Medium,
                    $"Ordem {workOrder.Number}", 0m,
                    "A origem contratual da ordem não está disponível, impedindo rastrear o valor contratado.",
                    "Revisar a ordem e revincular a origem contratual preservando o número emitido.");
            }
            else if (sourceDocument.ClientId is { } sourceClientId && sourceClientId != workOrder.ClientId)
            {
                Add(findings, workOrder.AccountId, workOrder.Id, accountName, "workorder-client-mismatch",
                    "Ordem de serviço com cliente divergente do orçamento", FinancialDivergenceSeverity.High,
                    $"Ordem {workOrder.Number}", 0m,
                    "O cliente da ordem difere do cliente do orçamento de origem, quebrando o vínculo comercial.",
                    "Corrigir o cliente da ordem com revisão autorizada e auditoria.");
            }

            if (workOrder.IsCancelled)
            {
                var activeOnCancelled = payments
                    .Where(x => x.WorkOrderId == workOrder.Id && x.AccountId == workOrder.AccountId && !x.IsReversed)
                    .ToArray();
                if (activeOnCancelled.Length > 0)
                {
                    Add(findings, workOrder.AccountId, workOrder.Id, accountName, "active-payment-on-cancelled-workorder",
                        "Recebimento ativo em ordem cancelada", FinancialDivergenceSeverity.High,
                        $"Ordem {workOrder.Number}", activeOnCancelled.Sum(x => x.Amount),
                        "Há recebimento ativo lastreado em uma ordem cancelada, distorcendo o realizado da conta.",
                        "Confirmar o serviço executado ou estornar o recebimento após revisão.");
                }
            }
        }

        foreach (var receipt in receipts)
        {
            if (receipt.IsDeleted || receipt.IsCancelled) continue;
            var accountName = ResolveAccountName(accountNames, receipt.AccountId);

            if (!paymentById.TryGetValue(receipt.PaymentId, out var payment) || payment.AccountId != receipt.AccountId)
            {
                Add(findings, receipt.AccountId, receipt.Id, accountName, "receipt-missing-payment",
                    "Recibo sem recebimento correspondente", FinancialDivergenceSeverity.High,
                    $"Recibo {receipt.Number}", receipt.Amount,
                    "O recibo não pode ser comprovado por um recebimento da mesma conta, ferindo a rastreabilidade.",
                    "Verificar o lançamento original e cancelar ou revincular o recibo com auditoria.");
                continue;
            }

            if (payment.IsReversed)
            {
                Add(findings, receipt.AccountId, receipt.Id, accountName, "receipt-for-reversed-payment",
                    "Recibo ativo de recebimento estornado", FinancialDivergenceSeverity.High,
                    $"Recibo {receipt.Number}", receipt.Amount,
                    "O recebimento foi estornado mas o recibo segue ativo, o que pode induzir o cliente a erro.",
                    "Cancelar o recibo preservando o registro original e registrar o motivo.");
            }
            else if (payment.Amount != receipt.Amount)
            {
                Add(findings, receipt.AccountId, receipt.Id, accountName, "receipt-amount-mismatch",
                    "Valor do recibo difere do recebimento", FinancialDivergenceSeverity.Medium,
                    $"Recibo {receipt.Number}", Math.Abs(payment.Amount - receipt.Amount),
                    "O valor emitido no recibo diverge do valor efetivamente recebido.",
                    "Emitir recibo correto e cancelar o divergente, mantendo o histórico.");
            }
        }

        return findings
            .OrderBy(x => x.Severity)
            .ThenByDescending(x => x.Amount)
            .ThenBy(x => x.AccountName, StringComparer.Ordinal)
            .ThenBy(x => x.FindingId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveAccountName(IReadOnlyDictionary<Guid, string>? accountNames, Guid accountId) =>
        accountNames is not null && accountNames.TryGetValue(accountId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name
            : "Conta " + accountId.ToString("N")[..ShortIdLength];

    private static string ResolveOrigin(FinancialDocumentRow? document, FinancialWorkOrderRow? workOrder, Guid paymentId)
    {
        if (workOrder is not null) return $"Ordem {workOrder.Number}";
        if (document is not null) return $"Orçamento {document.Number}";
        return "Recebimento " + paymentId.ToString("N")[..ShortIdLength];
    }

    private static void Add(List<FinancialIntegrityFinding> findings, Guid accountId, Guid occurrenceId,
        string accountName, string typeCode, string typeLabel, FinancialDivergenceSeverity severity, string origin,
        decimal amount, string risk, string recommendedAction) =>
        findings.Add(new FinancialIntegrityFinding(
            $"{typeCode}:{occurrenceId:N}", occurrenceId, accountId, accountName, typeCode, typeLabel, severity,
            origin, amount, risk, recommendedAction));
}
