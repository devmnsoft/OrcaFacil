using OrcaFacil.Application.Common;

namespace OrcaFacil.Application.Documents;

public sealed record QuoteWorkspaceQuery(string? Search = null, string? Status = null, Guid? ClientId = null,
    Guid? AssignedToUserId = null, DateTime? From = null, DateTime? To = null, decimal? Minimum = null, decimal? Maximum = null,
    string Sort = "newest", int Page = 1, int PageSize = 20);

public sealed record QuoteWorkspaceItem(Guid Id, string Number, string Status, string ClientName, decimal Total,
    DateTime IssueDate, DateTime? ValidUntil, DateTime CreatedAt, NextActionDescriptor NextAction,
    int RevisionNumber = 0, string? AssigneeName = null, decimal Paid = 0, decimal Balance = 0,
    Guid? WorkOrderId = null, string? WorkOrderNumber = null, decimal Reversed = 0,
    decimal Overpaid = 0, string ContractSource = "Document", IReadOnlyList<string>? FinancialWarnings = null);

public interface IQuoteWorkspaceService
{
    Task<OperationResult<PagedResult<QuoteWorkspaceItem>>> ListAsync(QuoteWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
