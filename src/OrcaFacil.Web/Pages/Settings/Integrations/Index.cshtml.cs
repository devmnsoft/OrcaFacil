using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Omnichannel;
using OrcaFacil.Application.Security;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Settings.Integrations;

[Authorize(Policy = "Permission:" + PermissionCodes.IntegrationsView)]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService current, IAuthorizationService authorization, IDataProtectionProvider protection, IAuditService audit) : PageModel
{
    private const string ProtectorPurpose = "OrcaFacil.Omnichannel.WhatsApp.v1";

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public WhatsAppInputModel WhatsApp { get; set; } = new();
    public bool SmtpPasswordConfigured { get; private set; }
    public bool WhatsAppAccessTokenConfigured { get; private set; }
    public bool WhatsAppAppSecretConfigured { get; private set; }
    public string? WhatsAppStatus { get; private set; }
    public string? WhatsAppLastFailure { get; private set; }
    public string? WhatsAppWebhookUrl { get; private set; }

    public sealed class InputModel { [Url] public string? PublicBaseUrl { get; set; } public string? WhatsAppNumber { get; set; } public string? EmailSignature { get; set; } [EmailAddress] public string? SupportEmail { get; set; } public string? SmtpHost { get; set; } [Range(1,65535)] public int? SmtpPort { get; set; } public string? SmtpUser { get; set; } [DataType(DataType.Password)] public string? SmtpPassword { get; set; } public bool SmtpUseSsl { get; set; } = true; public bool EmailSendingEnabled { get; set; } }

    /// <summary>
    /// Conta da Cloud API do WhatsApp (Meta). Access token e app secret são gravados
    /// protegidos e nunca voltam para a tela; os campos abaixo só indicam se já existem.
    /// </summary>
    public sealed class WhatsAppInputModel
    {
        [Required(ErrorMessage = "Informe o Phone Number ID da Meta.")]
        [StringLength(100)] public string PhoneNumberId { get; set; } = string.Empty;

        /// <summary>Business Account ID da Meta — identificador do provedor, colado do
        /// console dela (não é chave interna nossa; por isso o nome não carrega o sufixo Id).</summary>
        [StringLength(100)] public string? BusinessAccount { get; set; }

        [Required(ErrorMessage = "Informe o nome exibido da conta WhatsApp.")]
        [StringLength(200)] public string DisplayName { get; set; } = string.Empty;
        [DataType(DataType.Password)] public string? AccessToken { get; set; }
        [DataType(DataType.Password)] public string? AppSecret { get; set; }
        [Required(ErrorMessage = "Informe o verify token usado no handshake do webhook.")]
        [StringLength(200)] public string WebhookVerifyToken { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { if (!current.AccountId.HasValue) return Forbid(); var x=await db.IntegrationSettings.AsNoTracking().SingleOrDefaultAsync(x=>x.AccountId==current.AccountId&&!x.IsDeleted,ct); if(x is not null){ Input=new(){PublicBaseUrl=x.PublicBaseUrl,WhatsAppNumber=x.WhatsAppNumber,EmailSignature=x.EmailSignature,SupportEmail=x.SupportEmail,SmtpHost=x.SmtpHost,SmtpPort=x.SmtpPort,SmtpUser=x.SmtpUser,SmtpUseSsl=x.SmtpUseSsl,EmailSendingEnabled=x.EmailSendingEnabled}; SmtpPasswordConfigured=!string.IsNullOrEmpty(x.ProtectedSmtpPassword); WhatsAppWebhookUrl=WebhookUrl(x.PublicBaseUrl); } await LoadWhatsAppAsync(current.AccountId.Value, ct); return Page(); }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct) { if(!current.AccountId.HasValue || !(await authorization.AuthorizeAsync(User,"Permission:"+PermissionCodes.IntegrationsManage)).Succeeded) return Forbid(); if(Input.EmailSendingEnabled && (string.IsNullOrWhiteSpace(Input.SmtpHost)||!Input.SmtpPort.HasValue)){ModelState.AddModelError(string.Empty,"Informe host e porta SMTP antes de habilitar envios.");} if(!ModelState.IsValid){await LoadWhatsAppStateAsync(current.AccountId.Value,ct);return Page();} var id=current.AccountId.Value; var x=await db.IntegrationSettings.SingleOrDefaultAsync(x=>x.AccountId==id&&!x.IsDeleted,ct); if(x is null){x=new(){AccountId=id};db.Add(x);} x.PublicBaseUrl=Input.PublicBaseUrl?.TrimEnd('/');x.WhatsAppNumber=Digits(Input.WhatsAppNumber);x.EmailSignature=Input.EmailSignature?.Trim();x.SupportEmail=Input.SupportEmail?.Trim();x.SmtpHost=Input.SmtpHost?.Trim();x.SmtpPort=Input.SmtpPort;x.SmtpUser=Input.SmtpUser?.Trim();x.SmtpUseSsl=Input.SmtpUseSsl;x.EmailSendingEnabled=Input.EmailSendingEnabled;if(!string.IsNullOrWhiteSpace(Input.SmtpPassword))x.ProtectedSmtpPassword=protection.CreateProtector("OrcaFacil.Integrations.Smtp.v1").Protect(Input.SmtpPassword);await db.SaveChangesAsync(ct);await audit.RegisterAsync(current.UserId,"integrations.updated",nameof(IntegrationSetting),x.Id.ToString(),null,new{x.PublicBaseUrl,x.SmtpHost,x.SmtpPort,x.SmtpUseSsl,x.EmailSendingEnabled,PasswordChanged=!string.IsNullOrWhiteSpace(Input.SmtpPassword)},null,ct,id);TempData["Success"]="Integrações atualizadas. Nenhum envio de teste foi simulado.";return RedirectToPage(); }

    /// <summary>
    /// Cadastra/atualiza a conta da Cloud API. Nenhum teste de envio é executado aqui:
    /// a primeira prova real é o webhook do Meta (handshake GET + assinatura HMAC).
    /// Habilitar exige token e app secret — sem eles a assinatura nunca seria verificada.
    /// </summary>
    public async Task<IActionResult> OnPostWhatsAppAsync(CancellationToken ct)
    {
        if (!current.AccountId.HasValue || !(await authorization.AuthorizeAsync(User, "Permission:" + PermissionCodes.IntegrationsManage)).Succeeded)
            return Forbid();

        var id = current.AccountId.Value;
        var existing = await db.OmnichannelWhatsAppAccounts.SingleOrDefaultAsync(x => x.AccountId == id && !x.IsDeleted, ct);
        var hasToken = !string.IsNullOrWhiteSpace(WhatsApp.AccessToken) || !string.IsNullOrEmpty(existing?.ProtectedAccessToken);
        var hasSecret = !string.IsNullOrWhiteSpace(WhatsApp.AppSecret) || !string.IsNullOrEmpty(existing?.ProtectedAppSecret);

        // Habilitar exige os dois segredos: sem app secret a assinatura HMAC do webhook
        // nunca seria verificada e todo evento seria rejeitado.
        if (WhatsApp.IsEnabled && !(hasToken && hasSecret))
            ModelState.AddModelError("WhatsApp.IsEnabled", "Para habilitar, informe o access token e o app secret da aplicação da Meta.");

        if (!ModelState.IsValid)
        {
            await LoadWhatsAppStateAsync(id, ct);
            return Page();
        }

        var protector = protection.CreateProtector(ProtectorPurpose);
        var account = existing ?? db.OmnichannelWhatsAppAccounts.Add(new OmnichannelWhatsAppAccount { AccountId = id }).Entity;
        account.PhoneNumberId = WhatsApp.PhoneNumberId.Trim();
        account.BusinessAccountId = string.IsNullOrWhiteSpace(WhatsApp.BusinessAccount) ? null : WhatsApp.BusinessAccount.Trim();
        account.DisplayName = WhatsApp.DisplayName.Trim();
        account.WebhookVerifyToken = WhatsApp.WebhookVerifyToken.Trim();
        var tokenChanged = !string.IsNullOrWhiteSpace(WhatsApp.AccessToken);
        var secretChanged = !string.IsNullOrWhiteSpace(WhatsApp.AppSecret);
        if (tokenChanged) account.ProtectedAccessToken = protector.Protect(WhatsApp.AccessToken!.Trim());
        if (secretChanged) account.ProtectedAppSecret = protector.Protect(WhatsApp.AppSecret!.Trim());

        // Sem os dois segredos protegidos a conta não pode ficar habilitada, mesmo que o
        // formulário tenha sido marcado: o webhook seria rejeitado por assinatura.
        account.IsEnabled = WhatsApp.IsEnabled && hasToken && hasSecret;
        account.Status = account.IsEnabled ? OmnichannelChannelStatus.Configured.ToString() : "NotConfigured";
        if (account.IsEnabled) { account.LastFailure = null; account.LastHealthCheckAt = null; }
        account.Touch();

        await db.SaveChangesAsync(ct);
        await audit.RegisterAsync(current.UserId, "integrations.whatsapp_account.saved", nameof(OmnichannelWhatsAppAccount), account.Id.ToString(), null,
            new { account.PhoneNumberId, account.BusinessAccountId, account.DisplayName, account.IsEnabled, account.Status,
                 AccessTokenChanged = tokenChanged, AppSecretChanged = secretChanged }, null, ct, id);

        TempData["Success"] = account.IsEnabled
            ? "Conta do WhatsApp habilitada. Envios usam a Cloud API de verdade e falhas são registradas (nada é simulado); a primeira prova é o handshake do webhook com a Meta."
            : "Conta do WhatsApp salva e desabilitada. Nenhum envio será feito até habilitar com access token e app secret.";
        return RedirectToPage();
    }

    /// <summary>
    /// Indicadores de estado (segredos salvos, status, última falha). Carregados também
    /// quando o POST falha — ao contrário dos campos, que preservam o que foi digitado.
    /// </summary>
    private async Task LoadWhatsAppStateAsync(Guid accountId, CancellationToken ct)
    {
        var account = await db.OmnichannelWhatsAppAccounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        if (account is null) return;

        WhatsAppAccessTokenConfigured = !string.IsNullOrEmpty(account.ProtectedAccessToken);
        WhatsAppAppSecretConfigured = !string.IsNullOrEmpty(account.ProtectedAppSecret);
        WhatsAppStatus = account.Status;
        WhatsAppLastFailure = account.LastFailure;
    }

    /// <summary>Preenche os campos da tela a partir da conta salva (somente no GET).</summary>
    private async Task LoadWhatsAppAsync(Guid accountId, CancellationToken ct)
    {
        var account = await db.OmnichannelWhatsAppAccounts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);
        await LoadWhatsAppStateAsync(accountId, ct);
        if (account is null) return;

        // Segredos nunca voltam para a tela: só os campos públicos e o indicador.
        WhatsApp.PhoneNumberId = account.PhoneNumberId;
        WhatsApp.BusinessAccount = account.BusinessAccountId;
        WhatsApp.DisplayName = account.DisplayName;
        WhatsApp.WebhookVerifyToken = account.WebhookVerifyToken ?? string.Empty;
        WhatsApp.IsEnabled = account.IsEnabled;
    }

    private string? WebhookUrl(string? publicBaseUrl) =>
        string.IsNullOrWhiteSpace(publicBaseUrl) ? null : $"{publicBaseUrl.TrimEnd('/')}/api/webhooks/whatsapp";

    private static string? Digits(string? value)=>string.IsNullOrWhiteSpace(value)?null:new(value.Where(char.IsDigit).ToArray());
}
