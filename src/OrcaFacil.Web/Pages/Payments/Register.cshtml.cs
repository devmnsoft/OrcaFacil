using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Commercial;

namespace OrcaFacil.Web.Pages.Payments;
[Authorize]
public sealed class RegisterModel(IManualPaymentRegistrationService payments) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public void OnGet() => Input.PaidAt = DateTime.Now;
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var result = await payments.RegisterAsync(new(id, Input.Amount, Input.PaymentMethod, Input.PaidAt, Input.Notes, $"payment:{id}:{Input.IdempotencyKey}"), ct);
        if (!result.Succeeded) { ModelState.AddModelError(string.Empty, result.Message); return Page(); }
        return RedirectToPage("/Receipts/Create", new { paymentId = result.EntityId });
    }
    public sealed class InputModel { [Range(typeof(decimal), "0.01", "999999999")] public decimal Amount { get; set; } [Required] public string PaymentMethod { get; set; } = "Pix"; public DateTime PaidAt { get; set; } public string? Notes { get; set; } public Guid IdempotencyKey { get; set; } = Guid.NewGuid(); }
}
