using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Outsourcing;

public static class OutsourcingRules
{
    public static void ValidatePartnerCanReceiveWork(PartnerProfile partner, Guid accountId, bool allowBlocked = false)
    {
        EnsureAccount(accountId, partner.AccountId);
        if (partner.Status == PartnerStatus.Blocked && !allowBlocked) throw new InvalidOperationException("Parceiro bloqueado não pode receber nova solicitação.");
        if (partner.Status is not (PartnerStatus.Active or PartnerStatus.Preferred)) throw new InvalidOperationException("Somente parceiros ativos podem receber solicitações.");
    }

    public static void ValidateQuoteAcceptance(OutsourcingQuote quote, PartnerProfile partner, Guid accountId, DateTime now, bool allowBlocked = false)
    {
        EnsureAccount(accountId, quote.AccountId); EnsureAccount(accountId, partner.AccountId);
        if (quote.PartnerId != partner.Id) throw new UnauthorizedAccessException("A cotação não pertence ao parceiro.");
        if (quote.Status != OutsourcingQuoteStatus.Submitted) throw new InvalidOperationException("Somente cotação enviada pode ser aceita.");
        if (quote.ExpiresAt <= now) throw new InvalidOperationException("Cotação vencida não pode ser aceita.");
        if (quote.TotalAmount < 0 || quote.LeadTimeDays < 0) throw new InvalidOperationException("Valor e prazo não podem ser negativos.");
        ValidatePartnerCanReceiveWork(partner, accountId, allowBlocked);
    }

    public static void ValidateAssignmentIsUnique(IEnumerable<OutsourcingAssignment> assignments, Guid accountId, Guid workOrderId)
    {
        if (assignments.Any(x => !x.IsDeleted && x.AccountId == accountId && x.WorkOrderId == workOrderId && x.Status is not (OutsourcingAssignmentStatus.Canceled or OutsourcingAssignmentStatus.Rejected)))
            throw new InvalidOperationException("A OS já possui uma atribuição terceirizada ativa.");
    }

    public static void Accept(OutsourcingAssignment assignment, Guid accountId, Guid partnerId, DateTime now)
    {
        DemandAssignment(assignment, accountId, partnerId);
        if (assignment.Status != OutsourcingAssignmentStatus.Assigned) throw new InvalidOperationException("A atribuição não aguarda aceite.");
        assignment.Status = OutsourcingAssignmentStatus.Accepted; assignment.AcceptedAt = now; assignment.Touch();
    }

    public static void Reject(OutsourcingAssignment assignment, Guid accountId, Guid partnerId, string reason, DateTime now)
    {
        DemandAssignment(assignment, accountId, partnerId);
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A recusa exige motivo.");
        assignment.Status = OutsourcingAssignmentStatus.Rejected; assignment.RejectedAt = now; assignment.DecisionReason = reason.Trim(); assignment.Touch();
    }

    public static void ValidateEvidence(OutsourcingAssignment assignment, PartnerWorkOrderEvidence evidence, string contentType, long length)
    {
        DemandAssignment(assignment, evidence.AccountId, evidence.PartnerId);
        if (assignment.WorkOrderId != evidence.WorkOrderId) throw new UnauthorizedAccessException("A evidência não pertence à OS atribuída.");
        var allowed = new[] { "image/jpeg", "image/png", "application/pdf" };
        if (!allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase) || length is <= 0 or > 10 * 1024 * 1024)
            throw new InvalidOperationException("Arquivo de evidência inválido ou não permitido.");
    }

    public static void ValidatePaymentRequest(PartnerPaymentRequest payment, OutsourcingAssignment assignment)
    {
        DemandAssignment(assignment, payment.AccountId, payment.PartnerId);
        if (payment.OutsourcingAssignmentId != assignment.Id || payment.WorkOrderId != assignment.WorkOrderId) throw new UnauthorizedAccessException("Pagamento não pertence à atribuição.");
        if (payment.Amount <= 0) throw new InvalidOperationException("O valor deve ser maior que zero.");
        if (payment.Status == PartnerPaymentStatus.Paid || payment.PaidAt is not null || payment.PayableId is not null) throw new InvalidOperationException("Solicitação não pode nascer paga ou convertida.");
    }

    public static decimal RatingAverage(PartnerRating rating)
    {
        var scores = new[] { rating.QualityScore, rating.PunctualityScore, rating.CommunicationScore, rating.DeadlineScore, rating.DocumentationScore, rating.CostBenefitScore, rating.ClientSatisfactionScore };
        if (scores.Any(x => x is < 1 or > 5)) throw new InvalidOperationException("Notas devem estar entre 1 e 5.");
        return decimal.Round(scores.Average(), 2);
    }

    private static void DemandAssignment(OutsourcingAssignment assignment, Guid accountId, Guid partnerId)
    { EnsureAccount(accountId, assignment.AccountId); if (partnerId == Guid.Empty || assignment.PartnerId != partnerId) throw new UnauthorizedAccessException("A OS não pertence ao parceiro autenticado."); }
    private static void EnsureAccount(Guid expected, Guid actual) { if (expected == Guid.Empty || expected != actual) throw new UnauthorizedAccessException("O recurso não pertence à conta ativa."); }
}
