using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Clients;

[Authorize]
public sealed class DetailsModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly OrcaFacilDbContext _db;

    public DetailsModel(OrcaFacilDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public Client? Client { get; private set; }
    public IReadOnlyList<ClientDocumentDto> Budgets { get; private set; } = Array.Empty<ClientDocumentDto>();
    public IReadOnlyList<ClientDocumentDto> Receipts { get; private set; } = Array.Empty<ClientDocumentDto>();
    public decimal TotalBudgetAmount { get; private set; }
    public decimal TotalApprovedAmount { get; private set; }
    public ClientDocumentDto? LastDocument { get; private set; }
    public DateTime? LastContactAt { get; private set; }
    public string MaskedDocument => BrazilianDocument.Mask(Client?.DocumentType, Client?.DocumentNumber);

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        Client = await _db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserId == _current.UserId && !x.IsDeleted, ct);
        if (Client is null) return NotFound();

        var documentNumber = BrazilianDocument.Normalize(Client.DocumentNumber);
        var documentsQuery = _db.Documents.AsNoTracking().Where(d => d.UserId == _current.UserId && !d.IsDeleted &&
            (d.ClientName == Client.Name || (!string.IsNullOrWhiteSpace(documentNumber) && d.ClientDocument == documentNumber)));

        var documents = await documentsQuery
            .OrderByDescending(d => d.IssueDate)
            .Select(d => new ClientDocumentDto(d.Id, d.Type, d.Number, d.Status, d.Total, d.IssueDate, d.ClientDecision))
            .ToListAsync(ct);

        Budgets = documents.Where(d => d.Type == DocumentType.Budget).ToList();
        Receipts = documents.Where(d => d.Type == DocumentType.Receipt).ToList();
        TotalBudgetAmount = Budgets.Sum(d => d.Total);
        TotalApprovedAmount = Budgets.Where(d => d.ClientDecision == ClientDecision.Approved).Sum(d => d.Total);
        LastDocument = documents.FirstOrDefault();
        LastContactAt = LastDocument?.IssueDate ?? Client.UpdatedAt ?? Client.CreatedAt;
        return Page();
    }
}

public sealed record ClientDocumentDto(Guid Id, DocumentType Type, string Number, string Status, decimal Total, DateTime IssueDate, ClientDecision ClientDecision);
