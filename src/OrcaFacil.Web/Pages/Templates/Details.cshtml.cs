using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Templates;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly OrcaFacilDbContext _db;
    private readonly ICurrentAccountService _currentAccount;
    private readonly IAuditService _audit;

    public DetailsModel(OrcaFacilDbContext db, ICurrentAccountService currentAccount, IAuditService audit)
    {
        _db = db;
        _currentAccount = currentAccount;
        _audit = audit;
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

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        Template = await _db.BudgetTemplates
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted &&
                (x.IsSystemTemplate || (accountId.HasValue ? x.AccountId == accountId.Value : x.AccountId == null && x.UserId == userId)), ct);

        if (Template is null) return NotFound();

        var canManage = false;
        if (!Template.IsSystemTemplate)
        {
            if (accountId.HasValue && Template.AccountId == accountId.Value)
            {
                canManage = await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
            }
            else if (!accountId.HasValue && Template.AccountId == null && Template.UserId == userId)
            {
                canManage = true;
            }
        }

        CanEdit = canManage;
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

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }

            var canManage = await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
            if (!canManage) return Forbid();
        }

        var template = await _db.BudgetTemplates
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate &&
                (accountId.HasValue ? x.AccountId == accountId.Value : x.AccountId == null && x.UserId == userId), ct);

        if (template is null) return NotFound();

        // Validações explícitas
        if (string.IsNullOrWhiteSpace(Title))
            ModelState.AddModelError(nameof(Title), "Informe o nome do modelo.");
        else if (Title.Trim().Length > 120)
            ModelState.AddModelError(nameof(Title), "O nome do modelo aceita no máximo 120 caracteres.");

        if (Discount < 0)
            ModelState.AddModelError(nameof(Discount), "O desconto não pode ser negativo.");

        if (!string.IsNullOrWhiteSpace(PaymentMethod) && PaymentMethod.Trim().Length > 60)
            ModelState.AddModelError(nameof(PaymentMethod), "A forma de pagamento aceita no máximo 60 caracteres.");

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 1000)
            ModelState.AddModelError(nameof(Description), "A descrição aceita no máximo 1000 caracteres.");

        if (!string.IsNullOrWhiteSpace(ConditionsText) && ConditionsText.Trim().Length > 4000)
            ModelState.AddModelError(nameof(ConditionsText), "As condições comerciais aceitam no máximo 4000 caracteres.");

        if (!string.IsNullOrWhiteSpace(WarrantyText) && WarrantyText.Trim().Length > 2000)
            ModelState.AddModelError(nameof(WarrantyText), "A garantia aceita no máximo 2000 caracteres.");

        if (!ModelState.IsValid)
        {
            // Preserva os dados preenchidos pelo usuário sem recarregar do banco
            Template = template;
            CanEdit = true;
            return Page();
        }

        var before = new
        {
            template.Title,
            template.Description,
            template.ConditionsText,
            template.WarrantyText,
            template.PaymentMethod,
            template.Discount
        };

        template.Title = Title.Trim();
        template.Description = Description?.Trim() ?? string.Empty;
        template.ConditionsText = string.IsNullOrWhiteSpace(ConditionsText) ? null : ConditionsText.Trim();
        template.WarrantyText = string.IsNullOrWhiteSpace(WarrantyText) ? null : WarrantyText.Trim();
        template.PaymentMethod = string.IsNullOrWhiteSpace(PaymentMethod) ? null : PaymentMethod.Trim();
        template.Discount = Discount;
        template.Touch();

        await _audit.RegisterAsync(userId, "Update", "BudgetTemplate", template.Id.ToString(), before, new
        {
            template.Title,
            template.Description,
            template.ConditionsText,
            template.WarrantyText,
            template.PaymentMethod,
            template.Discount
        }, null, ct, accountId);

        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Modelo atualizado com sucesso.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, CancellationToken ct)
    {
        var accountId = _currentAccount.AccountId;
        var userId = _currentAccount.UserId;

        if (_currentAccount.HasAccount && accountId.HasValue)
        {
            try { await _currentAccount.EnsureAccountAccessAsync(ct); }
            catch (UnauthorizedAccessException) { return Forbid(); }

            var canManage = await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsEdit, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentTemplatesManage, ct) ||
                            await _currentAccount.HasPermissionAsync(PermissionCodes.DocumentsCreate, ct);
            if (!canManage) return Forbid();
        }

        var template = await _db.BudgetTemplates
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsSystemTemplate &&
                (accountId.HasValue ? x.AccountId == accountId.Value : x.AccountId == null && x.UserId == userId), ct);

        if (template is null) return NotFound();

        var before = new { template.IsActive };
        template.IsActive = !template.IsActive;
        template.Touch();

        await _audit.RegisterAsync(userId, "ToggleActive", "BudgetTemplate", template.Id.ToString(), before, new { template.IsActive }, null, ct, accountId);
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = template.IsActive ? "Modelo ativado com sucesso." : "Modelo inativado com sucesso.";
        return RedirectToPage(new { id });
    }
}
