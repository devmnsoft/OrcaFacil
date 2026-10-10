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
    IReadOnlyList<string> Warnings)
{
    public bool IsSettled => BalanceAmount == 0m && OverpaidAmount == 0m;
    public bool HasOverpayment => OverpaidAmount > 0m;
}

public interface ICommercialBalanceService
{
    Task<CommercialBalance?> GetForWorkOrderAsync(Guid accountId, Guid workOrderId, CancellationToken ct = default);
    Task<CommercialBalance?> GetForDocumentAsync(Guid accountId, Guid documentId, Guid? preferredWorkOrderId = null, CancellationToken ct = default);
}

public sealed record CommercialPaymentAmount(decimal Amount, bool IsReversed);

public static class CommercialBalanceCalculator
{
    public static CommercialBalance Calculate(Guid? documentId, Guid? workOrderId, string source, decimal contractedAmount,
        IEnumerable<CommercialPaymentAmount> payments, IReadOnlyList<string>? warnings = null)
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
            warnings ?? []);
    }
}
