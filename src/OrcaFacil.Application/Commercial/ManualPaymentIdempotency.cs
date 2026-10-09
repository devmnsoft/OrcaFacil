namespace OrcaFacil.Application.Commercial;

/// <summary>
/// Compares the commercial payload bound to a manual-payment idempotency key:
/// account is enforced by the query, and the stored command includes client, document, work order, amount, method and instant.
/// </summary>
public static class ManualPaymentIdempotency
{
    public static bool Matches(Guid? clientId, Guid? documentId, Guid? workOrderId, decimal amount, string methodCode, DateTime paidAtUtc,
        Guid? existingClientId, Guid? existingDocumentId, Guid? existingWorkOrderId, decimal existingAmount, string existingMethod, DateTime existingPaidAtUtc) =>
        clientId == existingClientId
        && documentId == existingDocumentId
        && workOrderId == existingWorkOrderId
        && amount == existingAmount
        && string.Equals(methodCode, existingMethod, StringComparison.Ordinal)
        && CommercialClock.SameUtcSecond(paidAtUtc, existingPaidAtUtc);

    public static bool Matches(Guid? workOrderId, decimal amount, string methodCode, DateTime paidAtUtc,
        Guid? existingWorkOrderId, decimal existingAmount, string existingMethod, DateTime existingPaidAtUtc) =>
        workOrderId == existingWorkOrderId
        && amount == existingAmount
        && string.Equals(methodCode, existingMethod, StringComparison.Ordinal)
        && CommercialClock.SameUtcSecond(paidAtUtc, existingPaidAtUtc);
}
