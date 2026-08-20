using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Approvals;

public sealed record ApprovalRequirement(bool Required, string? Reason);

public sealed class ApprovalWorkflowService
{
    public ApprovalRequirement Evaluate(DiscountPolicy? policy, decimal subtotal, decimal discount)
    {
        if (policy is null || !policy.IsActive) return new(false, null);
        var percentage = subtotal <= 0 ? 0 : discount / subtotal * 100m;
        if (percentage > policy.MaxDiscountPercentWithoutApproval)
            return new(true, $"Desconto de {percentage:N2}% excede o limite de {policy.MaxDiscountPercentWithoutApproval:N2}%.");
        if (discount > policy.MaxDiscountAmountWithoutApproval)
            return new(true, "O valor do desconto excede o limite da política.");
        if (policy.RequiresApprovalAboveAmount is decimal ceiling && subtotal - discount > ceiling)
            return new(true, "O valor total excede o limite da política.");
        return new(false, null);
    }

    public void Approve(ApprovalRequest request, Guid actorUserId)
    {
        EnsurePending(request);
        if (request.RequireDifferentApprover && request.RequestedByUserId == actorUserId)
            throw new InvalidOperationException("O solicitante não pode aprovar a própria solicitação.");
        if (request.ApproverUserId.HasValue && request.ApproverUserId != actorUserId)
            throw new UnauthorizedAccessException("A solicitação está atribuída a outro aprovador.");
        request.ApproverUserId = actorUserId; request.Status = ApprovalStatus.Approved; request.DecidedAt = DateTime.UtcNow; request.Touch();
    }

    public void Reject(ApprovalRequest request, Guid actorUserId, string reason)
    {
        EnsurePending(request);
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Informe o motivo da recusa.", nameof(reason));
        if (request.ApproverUserId.HasValue && request.ApproverUserId != actorUserId) throw new UnauthorizedAccessException("A solicitação está atribuída a outro aprovador.");
        request.ApproverUserId = actorUserId; request.Status = ApprovalStatus.Rejected; request.Reason = reason.Trim(); request.DecidedAt = DateTime.UtcNow; request.Touch();
    }

    public void EnsurePublicProposalAllowed(ApprovalRequest? request)
    {
        if (request is not null && request.Status != ApprovalStatus.Approved)
            throw new InvalidOperationException("Este orçamento precisa de aprovação interna antes de ser enviado ao cliente.");
    }

    private static void EnsurePending(ApprovalRequest request)
    { if (request.Status != ApprovalStatus.Pending) throw new InvalidOperationException("A solicitação já foi decidida."); }
}
