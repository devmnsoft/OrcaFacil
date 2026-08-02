using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Receipts;

[Authorize]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    public IReadOnlyList<Receipt> Receipts { get; private set; } = [];
    public int TotalItems { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? ClientId { get; set; }
    [BindProperty(SupportsGet = true)] public string? PaymentMethod { get; set; }
    [BindProperty(SupportsGet = true)] public ReceiptOriginType? OriginType { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinimumAmount { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaximumAmount { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

    public async Task OnGetAsync(CancellationToken ct)
    {
        PageNumber = Math.Max(1, PageNumber);
        PageSize = Math.Clamp(PageSize, 10, 100);
        var query = db.Receipts.AsNoTracking()
            .Where(receipt => receipt.AccountId == account.AccountId && !receipt.IsDeleted);

        if (From.HasValue) query = query.Where(receipt => receipt.IssuedAt >= From.Value);
        if (To.HasValue) query = query.Where(receipt => receipt.IssuedAt < To.Value.AddDays(1));
        if (ClientId.HasValue) query = query.Where(receipt => receipt.ClientId == ClientId.Value);
        if (OriginType.HasValue) query = query.Where(receipt => receipt.OriginType == OriginType.Value);
        if (MinimumAmount.HasValue) query = query.Where(receipt => receipt.Amount >= MinimumAmount.Value);
        if (MaximumAmount.HasValue) query = query.Where(receipt => receipt.Amount <= MaximumAmount.Value);
        if (!string.IsNullOrWhiteSpace(PaymentMethod) && PaymentMethodCodes.TryParse(PaymentMethod, out var method))
        {
            var canonical = method.ToCode();
            var legacyLabel = method.ToLabel();
            query = query.Where(receipt => receipt.PaymentMethod == canonical || receipt.PaymentMethod == legacyLabel);
        }
        if (string.Equals(Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            query = query.Where(receipt => receipt.CancelledAt != null);
        else if (string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(receipt => receipt.CancelledAt == null);

        TotalItems = await query.CountAsync(ct);
        if (PageNumber > TotalPages) PageNumber = TotalPages;
        Receipts = await query.OrderByDescending(receipt => receipt.IssuedAt)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);
    }
}
