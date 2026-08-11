using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages;

[EnableRateLimiting("public")]
public sealed class IndexModel(OrcaFacilDbContext db) : PageModel
{
    [BindProperty]
    public LeadInput Input { get; set; } = new();

    public sealed class LeadInput : IValidatableObject
    {
        [Required(ErrorMessage = "Informe seu nome."), StringLength(140)]
        public string Name { get; set; } = string.Empty;
        [StringLength(180)] public string? CompanyName { get; set; }
        [EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(254)] public string? Email { get; set; }
        [StringLength(40)] public string? Phone { get; set; }
        [StringLength(100)] public string? Segment { get; set; }
        [Range(0, 1000000, ErrorMessage = "Informe um volume válido.")] public int? MonthlyBudgetVolume { get; set; }
        [StringLength(1200, ErrorMessage = "A mensagem deve ter no máximo 1.200 caracteres.")] public string? Message { get; set; }
        [Range(typeof(bool), "true", "true", ErrorMessage = "Você precisa autorizar o contato para enviar.")]
        public bool ConsentAccepted { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Phone))
                yield return new ValidationResult("Informe um telefone/WhatsApp ou e-mail.", [nameof(Email), nameof(Phone)]);
        }
    }

    public async Task<IActionResult> OnPostLeadAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var lead = new CommercialLead
        {
            Name = Input.Name.Trim(),
            CompanyName = Clean(Input.CompanyName),
            Email = Clean(Input.Email)?.ToLowerInvariant(),
            Phone = Clean(Input.Phone),
            Segment = Clean(Input.Segment),
            MonthlyBudgetVolume = Input.MonthlyBudgetVolume,
            Message = Clean(Input.Message),
            ConsentAccepted = Input.ConsentAccepted,
            SourcePage = "/"
        };
        db.CommercialLeads.Add(lead);

        var administrators = await db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && !x.IsBlocked &&
                x.Role == UserRole.SuperAdmin)
            .Select(x => x.Id).ToListAsync(ct);
        foreach (var userId in administrators)
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Novo contato comercial",
                Message = $"Uma nova solicitação de {lead.Name} chegou pela Home.",
                Type = NotificationType.Info,
                Category = NotificationCategory.System,
                ActionUrl = $"/Admin/Leads/Details?id={lead.Id}",
                ActionText = "Ver lead"
            });

        await db.SaveChangesAsync(ct);
        TempData.Success("Solicitação enviada", "A equipe MNSOFT vai entrar em contato.");
        return RedirectToPage("/Index", null, "contato");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
