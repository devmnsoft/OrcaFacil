namespace OrcaFacil.Application.Saas.Billing;

public sealed record AccountModuleBillingLine(string ModuleCode, string DisplayName, string Status, string Period,
    decimal ContractedPrice, decimal Discount, decimal EffectivePrice);
public sealed record AccountModuleBillingPreview(Guid AccountId, IReadOnlyList<AccountModuleBillingLine> Lines,
    decimal Subtotal, decimal Discount, decimal Total, string Currency);

public interface IAccountModuleBillingService
{
    Task<AccountModuleBillingPreview> PreviewAsync(Guid accountId, CancellationToken ct = default);
}

public sealed class SaasBillingService(IAccountModuleBillingService accounts)
{
    public Task<AccountModuleBillingPreview> PreviewAccountAsync(Guid accountId, CancellationToken ct = default) =>
        accounts.PreviewAsync(accountId, ct);
}
