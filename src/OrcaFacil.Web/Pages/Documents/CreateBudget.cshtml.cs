using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Documents;

[Authorize]
public class CreateBudgetModel : PageModel
{
    private readonly ICurrentUserService _current;
    private readonly DocumentService _service;
    private readonly OrcaFacilDbContext _db;

    public CreateBudgetModel(ICurrentUserService current, DocumentService service, OrcaFacilDbContext db)
    {
        _current = current;
        _service = service;
        _db = db;
    }

    [BindProperty] public DocumentForm Input { get; set; } = DocumentForm.Default();
    public Guid? LoadedTemplateId { get; private set; }
    public string? LoadedTemplateTitle { get; private set; }

    public async Task OnGetAsync(Guid? templateId, CancellationToken ct)
    {
        if (templateId is null) return;
        var template = await _db.BudgetTemplates.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == templateId && x.IsActive && !x.IsDeleted && (x.IsSystemTemplate || x.UserId == _current.UserId), ct);
        if (template is null)
        {
            TempData.Warning("Não encontramos este modelo. Escolha outro ou comece em branco.");
            return;
        }
        LoadedTemplateId = template.Id;
        LoadedTemplateTitle = template.Title;
        Input = new DocumentForm
        {
            Notes = $"Orçamento criado a partir do modelo {template.Title}. Revise prazos, condições de pagamento e valores antes do envio.",
            Items = template.Items.OrderBy(i => i.SortOrder).Select(i => new DocumentItemForm { Description = i.Description, Quantity = i.Quantity, UnitPrice = i.UnitPrice }).ToList()
        };
        TempData.Success("Modelo carregado. Revise os itens e ajuste os valores antes de salvar.");
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var cmd = new CreateDocumentCommand(_current.UserId, DocumentType.Budget, "", Input.ClientName, Input.ToItems(), Input.Discount, Input.Notes);
        var r = await _service.CreateBudgetAsync(cmd, ct);
        if (!r.Succeeded)
        {
            ModelState.AddModelError("", r.Error ?? "Erro ao criar orçamento.");
            return Page();
        }
        TempData.Success("Orçamento criado a partir de modelo ou formulário guiado. Gere o PDF no histórico.");
        return RedirectToPage("/Documents/Details", new { id = r.Value });
    }
}

public class DocumentForm
{
    public string ClientName { get; set; } = "";
    public decimal Discount { get; set; }
    public string? Notes { get; set; }
    public List<DocumentItemForm> Items { get; set; } = [];
    public static DocumentForm Default() => new() { Items = [new()] };
    public IReadOnlyList<DocumentItemDto> ToItems() => Items.Where(i => !string.IsNullOrWhiteSpace(i.Description)).Select(i => new DocumentItemDto(i.Description, i.Quantity, i.UnitPrice, i.Discount)).ToList();
}

public class DocumentItemForm
{
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}
