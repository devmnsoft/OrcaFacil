using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentAccountService _currentAccount;

    public DetailsModel(OrcaFacilDbContext db, ICurrentAccountService currentAccount)
    {
        _db = db;
        _currentAccount = currentAccount;
    }

    public BudgetTemplate? Template { get; private set; }
    public bool CanEdit { get; private set; }

    [BindProperty]
    public string Title { get; set; } = string.Empty;
    [BindProperty]
    public string? Description { get; set; }
    [BindProperty]
    public string? ConditionsText { get; set; }
    [BindProperty]
    public string? WarrantyText { get; set; }
    [BindProperty]
    public string? PaymentMethod { get; set; }
    [BindProperty]
    public decimal Discount { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        Template = await _db.BudgetTemplates
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && (x.IsSystemTemplate || x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)), ct);

        if (Template is null) return NotFound();

        CanEdit = !Template.IsSystemTemplate && (Template.AccountId == accountId || (Template.AccountId == null && Template.UserId == userId));
        Title = Template.Title;
        Description = Template.Description;
        ConditionsText = Template.ConditionsText;
        WarrantyText = Template.WarrantyText;
        PaymentMethod = Template.PaymentMethod;
        Discount = Template.Discount;

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        var template = await _db.BudgetTemplates
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate && (x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)), ct);

        if (template is null) return NotFound();

        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "Informe o nome do modelo.");
            return await OnGetAsync(id, ct);
        }

        template.Title = Title.Trim();
        template.Description = Description ?? string.Empty;
        template.ConditionsText = ConditionsText;
        template.WarrantyText = WarrantyText;
        template.PaymentMethod = PaymentMethod;
        template.Discount = Math.Max(0, Discount);
        template.Touch();

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Modelo atualizado com sucesso.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        var template = await _db.BudgetTemplates
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate && (x.AccountId == accountId || (x.AccountId == null && x.UserId == userId)), ct);

        if (template is null) return NotFound();

        template.IsActive = !template.IsActive;
        template.Touch();
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = template.IsActive ? "Modelo ativado com sucesso." : "Modelo inativado com sucesso.";
        return RedirectToPage(new { id });
    }
}
