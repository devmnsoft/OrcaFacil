namespace OrcaFacil.Application.Commercial;

/// <summary>
/// Compares the commercial payload bound to a manual-payment idempotency key:
/// account is enforced by the query, and the stored command is work order, amount, method and instant.
/// </summary>
public static class ManualPaymentIdempotency
{
    public static bool Matches(Guid? workOrderId, decimal amount, string methodCode, DateTime paidAtUtc,
        Guid? existingWorkOrderId, decimal existingAmount, string existingMethod, DateTime existingPaidAtUtc) =>
        workOrderId == existingWorkOrderId
        && amount == existingAmount
        && string.Equals(methodCode, existingMethod, StringComparison.Ordinal)
        && CommercialClock.SameUtcSecond(paidAtUtc, existingPaidAtUtc);
}
