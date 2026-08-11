namespace OrcaFacil.Application.Documents;

public sealed record BudgetStartClient(Guid Id, string Name, string Detail, string SearchText);
public sealed record BudgetStartService(Guid Id, string Name, string? Description, string Unit, decimal Price, string SearchText);
public sealed record BudgetStartTemplate(Guid Id, string Title, string Profession, int ItemCount);
public sealed record BudgetStartDraft(Guid Id, string Number, string ClientName, decimal Total, DateTime ChangedAt);
public sealed record BudgetStartEmptyState(string Title, string Description, string ActionLabel, string ActionPage);
public sealed record GuidedBudgetStartView(
    IReadOnlyList<BudgetStartClient> Clients,
    IReadOnlyList<BudgetStartService> Services,
    IReadOnlyList<BudgetStartTemplate> Templates,
    IReadOnlyList<BudgetStartDraft> Drafts,
    IReadOnlyDictionary<string, BudgetStartEmptyState> EmptyStates);

public interface IGuidedBudgetStartService
{
    Task<GuidedBudgetStartView> GetAsync(CancellationToken cancellationToken = default);
}
