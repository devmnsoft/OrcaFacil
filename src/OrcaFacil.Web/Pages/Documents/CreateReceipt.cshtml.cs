using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrcaFacil.Web.Pages.Documents;

/// <summary>Compatibility endpoint. Receipt creation now has an independent journey.</summary>
[Authorize]
public sealed class CreateReceiptModel : PageModel
{
    public IActionResult OnGet(Guid? clientId, Guid? workOrderId, Guid? paymentId, Guid? documentId) =>
        RedirectToPage("/Receipts/Create", new { clientId, workOrderId, paymentId, documentId });
}
