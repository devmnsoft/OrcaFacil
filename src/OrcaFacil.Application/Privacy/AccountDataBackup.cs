using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Privacy;

// ---- Explicit scalar-only projections of the account's business records. They carry no EF navigation
// properties, so they serialize to a stable, cycle-free JSON bundle that the account owner can keep or move.
public sealed record ClientBackup(Guid Id, string Name, string PersonType, string? DocumentType, string? DocumentNumber, string? Email, string? Phone, string? City, string? Address, bool IsActive);
public sealed record ServiceBackup(Guid Id, string? Code, string Name, decimal StandardPrice, bool IsActive, bool IsRecurring);
public sealed record DocumentItemBackup(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal Discount);
public sealed record DocumentBackup(Guid Id, string Type, string Number, string Status, string ClientName, Guid? ClientId, DateTime IssueDate, decimal Subtotal, decimal Discount, decimal Total, string? PaymentMethod, List<DocumentItemBackup> Items);
public sealed record WorkOrderBackup(Guid Id, string Number, string Title, Guid ClientId, string Status, DateOnly? ServiceCompetence, DateTime? ScheduledStart, DateTime? CompletedAt, decimal TotalSnapshot, bool PaymentReceived);
public sealed record ReceiptBackup(Guid Id, string Number, Guid ClientId, decimal Amount, string PaymentMethod, DateTime IssuedAt, DateTime? CancelledAt, string? ServiceDescription);
public sealed record FinancialEntryBackup(Guid Id, Guid ClientId, string Description, DateOnly DueDate, decimal Amount, decimal PaidAmount, string Status);
public sealed record ContractBackup(Guid Id, string Number, string Title, Guid ClientId, string Status, DateOnly StartDate, DateOnly? EndDate, decimal RecurringAmount, string Periodicity, DateTime? CanceledAt);
public sealed record FiscalNoteBackup(Guid Id, string FiscalDocumentType, string? Number, DateTime? IssuedAt, string Status, string? ProviderName, string? KeyMasked);

/// <summary>The assembled, account-scoped data bundle. Secrets are stripped by the downstream sanitizer; this only shapes.</summary>
public sealed record AccountDataBackup(
    int SchemaVersion,
    List<ClientBackup> Clients,
    List<ServiceBackup> Services,
    List<DocumentBackup> Documents,
    List<WorkOrderBackup> WorkOrders,
    List<ReceiptBackup> Receipts,
    List<FinancialEntryBackup>? FinancialEntries, // null when financial data was not included
    List<ContractBackup> Contracts,
    List<FiscalNoteBackup> FiscalNotes);

/// <summary>
/// Pure shaping of an account's records into <see cref="AccountDataBackup"/>. Free of the DbContext so the
/// financial-inclusion rule, the scope tag and the record counts are unit-testable like the other rule sets.
/// </summary>
public static class AccountDataBackupShaper
{
    /// <summary>Bump when the bundle shape changes so older exports stay interpretable on re-import.</summary>
    public const int SchemaVersion = 1;

    public static AccountDataBackup Shape(
        IEnumerable<Client> clients,
        IEnumerable<ServiceCatalogItem> services,
        IEnumerable<Document> documents,
        IEnumerable<WorkOrder> workOrders,
        IEnumerable<Receipt> receipts,
        bool includesFinancial,
        IEnumerable<FinancialEntry>? financialEntries,
        IEnumerable<RecurringContract> contracts,
        IEnumerable<FiscalDocumentRequest> fiscalNotes) => new(
        SchemaVersion,
        clients.Select(c => new ClientBackup(c.Id, c.Name, c.PersonType.ToString(), c.DocumentType?.ToString(), c.DocumentNumber, c.Email, c.Phone, c.City, c.Address, c.IsActive)).ToList(),
        services.Select(s => new ServiceBackup(s.Id, s.Code, s.Name, s.StandardPrice, s.IsActive, s.IsRecurring)).ToList(),
        documents.Select(d => new DocumentBackup(d.Id, d.Type.ToString(), d.Number, d.Status, d.ClientName, d.ClientId, d.IssueDate, d.Subtotal, d.Discount, d.Total, d.PaymentMethod,
            d.Items?.OrderBy(i => i.SortOrder).Select(i => new DocumentItemBackup(i.Id, i.Description, i.Quantity, i.UnitPrice, i.Discount)).ToList() ?? [])).ToList(),
        workOrders.Select(w => new WorkOrderBackup(w.Id, w.Number, w.Title, w.ClientId, w.Status.ToString(), w.ServiceCompetence, w.ScheduledStart, w.CompletedAt, w.TotalSnapshot, w.PaymentReceived)).ToList(),
        receipts.Select(r => new ReceiptBackup(r.Id, r.Number, r.ClientId, r.Amount, r.PaymentMethod, r.IssuedAt, r.CancelledAt, r.ServiceDescription)).ToList(),
        includesFinancial && financialEntries is not null
            ? financialEntries.Select(f => new FinancialEntryBackup(f.Id, f.ClientId, f.Description, f.DueDate, f.Amount, f.PaidAmount, f.Status.ToString())).ToList()
            : null,
        contracts.Select(c => new ContractBackup(c.Id, c.Number, c.Title, c.ClientId, c.Status.ToString(), c.StartDate, c.EndDate, c.RecurringAmount, c.Periodicity.ToString(), c.CanceledAt)).ToList(),
        fiscalNotes.Select(n => new FiscalNoteBackup(n.Id, n.FiscalDocumentType, n.ExternalNumber, n.IssuedAt, n.Status.ToString(), n.ProviderName, n.ExternalKeyMasked)).ToList());

    /// <summary>Total number of records in the bundle; financial is counted only when it was included.</summary>
    public static int TotalRecords(AccountDataBackup bundle) =>
        bundle.Clients.Count + bundle.Services.Count + bundle.Documents.Count + bundle.WorkOrders.Count + bundle.Receipts.Count
        + (bundle.FinancialEntries?.Count ?? 0) + bundle.Contracts.Count + bundle.FiscalNotes.Count;

    /// <summary>Scope recorded on the job/export: full when financial data is present, otherwise core.</summary>
    public static string Scope(bool includesFinancial) => includesFinancial ? "account.full" : "account.core";
}
