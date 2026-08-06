using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Payments;

[Authorize]
public sealed class RegisterModel(
    IManualPaymentRegistrationService payments,
    OrcaFacilDbContext db,
    ICurrentAccountService account) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public WorkOrder? WorkOrder { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal Balance => Math.Max(0m, (WorkOrder?.TotalSnapshot ?? 0m) - PaidAmount);

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        Input.PaidAt = DateTime.Now;
        Input.Amount = Balance;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        if (Input.Amount > Balance)
            ModelState.AddModelError("Input.Amount", $"Informe no máximo o saldo de {Balance:C}.");
        if (!ModelState.IsValid) return Page();

        var result = await payments.RegisterAsync(new(id, Input.Amount, Input.PaymentMethod, Input.PaidAt,
            Input.Notes, $"payment:{id}:{Input.IdempotencyKey}"), ct);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }
        return RedirectToPage("/Receipts/Create", new { paymentId = result.EntityId });
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken ct)
    {
        WorkOrder = await db.WorkOrders.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.AccountId == account.AccountId && !x.IsDeleted, ct);
        if (WorkOrder is null) return false;
        PaidAmount = await db.ManualPayments.AsNoTracking()
            .Where(x => x.AccountId == account.AccountId && x.WorkOrderId == id && !x.IsDeleted && x.Status == FinancialRecordStatus.Active)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        return true;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Informe o valor recebido.")]
        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Informe um valor maior que zero.")]
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "Escolha a forma de pagamento.")]
        public string PaymentMethod { get; set; } = "Pix";
        [Required(ErrorMessage = "Informe a data do recebimento.")]
        public DateTime PaidAt { get; set; }
        [StringLength(1000, ErrorMessage = "A observação deve ter no máximo 1.000 caracteres.")]
        public string? Notes { get; set; }
        public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    }
}
