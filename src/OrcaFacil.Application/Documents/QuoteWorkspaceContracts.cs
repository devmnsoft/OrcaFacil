using OrcaFacil.Application.Common;

namespace OrcaFacil.Application.Documents;

public sealed record QuoteWorkspaceQuery(string? Search = null, string? Status = null, Guid? ClientId = null,
    DateTime? From = null, DateTime? To = null, decimal? Minimum = null, decimal? Maximum = null,
    string Sort = "newest", int Page = 1, int PageSize = 20);

public sealed record QuoteWorkspaceItem(Guid Id, string Number, string Status, string ClientName, decimal Total,
    DateTime IssueDate, DateTime? ValidUntil, DateTime CreatedAt, NextActionDescriptor NextAction);

public interface IQuoteWorkspaceService
{
    Task<OperationResult<PagedResult<QuoteWorkspaceItem>>> ListAsync(QuoteWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
