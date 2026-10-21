using System.Security.Cryptography;using System.Text;using System.Text.Json;using Microsoft.AspNetCore.DataProtection;using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.Logging;using Microsoft.Extensions.Options;using OrcaFacil.Application.Abstractions;using OrcaFacil.Application.Accounts;using OrcaFacil.Application.DTOs;using OrcaFacil.Shared;using OrcaFacil.Domain.Enums;using OrcaFacil.Domain.ValueObjects;using OrcaFacil.Domain.Entities;using OrcaFacil.Persistence;using OrcaFacil.Web.Email;
namespace OrcaFacil.Web.Security;
public interface IAccountInvitationService
{
 /// <summary>Cria o convite (hash do cru, cru nunca persistido) e enfileira o e-mail na outbox. Erro retorna Result.Fail, nunca exceção esperada.</summary>
 Task<Result<Guid>> CreateAsync(Guid accountId,string email,string fullName,string roleCode,Guid invitedByUserId,string correlationId,CancellationToken ct=default);
 /// <summary>Convites pendentes da conta (para contagem de vagas e revogação por reemissão).</summary>
 Task RevokePendingForEmailAsync(Guid accountId,string email,CancellationToken ct=default);
 Task<int> CountPendingAsync(Guid accountId,CancellationToken ct=default);
 /// <summary>Dados de exibição do aceite; falha quando token é inválido, expirado, usado ou conta/usuário convidante sumiram.</summary>
 Task<Result<InvitationAcceptView>> FindPendingAsync(string token,CancellationToken ct=default);
 /// <summary>Aceite único e serializável: usuário existente entra na conta; novo usuário define a própria senha. Define o login já autenticado.</summary>
 Task<Result<UserSummaryDto>> AcceptAsync(string token,string password,bool acceptTerms,string correlationId,CancellationToken ct=default);
}
public sealed record InvitationAcceptView(string FullName,string Email,string AccountName,string RoleCode);
public sealed class AccountInvitationService(OrcaFacilDbContext db,IPasswordResetTokenService tokens,IDataProtectionProvider protection,IOptions<ApplicationUrlOptions> appOptions,IOptions<SecuritySecretOptions> secrets,IPasswordHasher hasher,ILogger<AccountInvitationService> logger):IAccountInvitationService
{
 public async Task<Result<Guid>> CreateAsync(Guid accountId,string email,string fullName,string roleCode,Guid invitedByUserId,string correlationId,CancellationToken ct=default)
 {
  if(accountId==Guid.Empty||invitedByUserId==Guid.Empty)return Result<Guid>.Fail("Convite inválido.");
  if(string.IsNullOrWhiteSpace(fullName))return Result<Guid>.Fail("Informe o nome de quem recebe o convite.");
  var normalized=email.Trim().ToLowerInvariant();
  try{new OrcaFacil.Domain.ValueObjects.Email(normalized);}catch(ArgumentException){return Result<Guid>.Fail("Informe um e-mail válido.");}
  var account=await db.BusinessAccounts.SingleOrDefaultAsync(x=>x.Id==accountId&&!x.IsDeleted,ct);
  if(account is null)return Result<Guid>.Fail("Conta não encontrada.");
  if(await (from m in db.AccountMembers join u in db.Users on m.UserId equals u.Id where m.AccountId==accountId&&u.Email==normalized&&!m.IsDeleted&&!u.IsDeleted select m.Id).AnyAsync(ct))return Result<Guid>.Fail("Este e-mail já pertence à conta.");
  if(await db.Users.AnyAsync(x=>x.Email==normalized&&!x.IsDeleted,ct))return Result<Guid>.Fail("Este e-mail já tem acesso ao Fechô. Use o vínculo direto com senha temporária.");
  await RevokePendingForEmailAsync(accountId,normalized,ct); // reemissão: convites anteriores do mesmo e-mail expiram, histórico intacto
  var now=DateTime.UtcNow;
  var raw=tokens.Generate();
  var invite=new AccountInvitation{AccountId=accountId,Email=normalized,FullName=fullName.Trim(),RoleCode=roleCode,InvitedByUserId=invitedByUserId,TokenHash=tokens.Hash(raw),ExpiresAt=now.AddDays(AccountInvitationRules.ValidityDays),RequestedCorrelationId=correlationId};
  db.AccountInvitations.Add(invite);
  db.EmailOutboxMessages.Add(BuildOutbox(now,accountId,normalized,$"{appOptions.Value.PublicBaseUrl.TrimEnd('/')}/Auth/AcceptInvite?token={Uri.EscapeDataString(raw)}",account.DisplayName,roleCode,fullName.Trim(),invite.ExpiresAt,invite.Id));
  try{await db.SaveChangesAsync(ct);}
  catch(DbUpdateException ex){logger.LogError(ex,"ACCOUNT_INVITATION_PERSIST_FAILED AccountId {AccountId} CorrelationId {CorrelationId}",accountId,correlationId);return Result<Guid>.Fail("Não foi possível criar o convite agora. Tente novamente.");}
  logger.LogInformation("ACCOUNT_INVITATION_CREATED InvitationId {InvitationId} AccountId {AccountId} CorrelationId {CorrelationId}",invite.Id,accountId,correlationId);
  return Result<Guid>.Ok(invite.Id);
 }
 public async Task RevokePendingForEmailAsync(Guid accountId,string email,CancellationToken ct=default)
 {var cutoff=DateTime.UtcNow;var normalized=email.Trim().ToLowerInvariant();foreach(var invite in await db.AccountInvitations.Where(x=>x.AccountId==accountId&&x.Email==normalized&&!x.IsDeleted&&x.UsedAt==null&&x.ExpiresAt>cutoff).ToListAsync(ct))invite.Revoke(cutoff);}
 public Task<int> CountPendingAsync(Guid accountId,CancellationToken ct=default)
 {var cutoff=DateTime.UtcNow;return db.AccountInvitations.CountAsync(x=>x.AccountId==accountId&&!x.IsDeleted&&x.UsedAt==null&&x.ExpiresAt>cutoff,ct);}
 public async Task<Result<InvitationAcceptView>> FindPendingAsync(string token,CancellationToken ct=default)
 {
  if(string.IsNullOrWhiteSpace(token)||token.Length>200)return Result<InvitationAcceptView>.Fail("Convite inválido.");
  var hash=tokens.Hash(token);
  var invite=await db.AccountInvitations.AsNoTracking().SingleOrDefaultAsync(x=>x.TokenHash==hash,ct);
  if(invite?.IsAvailable(DateTime.UtcNow)!=true)return Result<InvitationAcceptView>.Fail("Este convite já foi utilizado, expirou ou foi revogado. Peça ao administrador um novo.");
  var account=await db.BusinessAccounts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==invite.AccountId&&!x.IsDeleted,ct);
  if(account is null)return Result<InvitationAcceptView>.Fail("Este convite já não está válido. Peça ao administrador um novo.");
  return Result<InvitationAcceptView>.Ok(new InvitationAcceptView(invite.FullName,invite.Email,account.DisplayName,invite.RoleCode));
 }
 public async Task<Result<UserSummaryDto>> AcceptAsync(string token,string password,bool acceptTerms,string correlationId,CancellationToken ct=default)
 {
  if(!acceptTerms)return Result<UserSummaryDto>.Fail("É preciso aceitar os termos de uso e a política de privacidade.");
  if(string.IsNullOrWhiteSpace(token)||token.Length>200)return Result<UserSummaryDto>.Fail("Convite inválido.");
  var hash=tokens.Hash(token);var now=DateTime.UtcNow;
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  var invite=await db.AccountInvitations.SingleOrDefaultAsync(x=>x.TokenHash==hash,ct);
  if(invite?.IsAvailable(now)!=true)return Result<UserSummaryDto>.Fail("Este convite já foi utilizado, expirou ou foi revogado. Peça ao administrador um novo.");
  var account=await db.BusinessAccounts.SingleAsync(x=>x.Id==invite.AccountId&&!x.IsDeleted,ct);
  var user=await db.Users.SingleOrDefaultAsync(x=>x.Email==invite.Email&&!x.IsDeleted,ct);
  if(user is null)
  {
   user=new UserAccount{Name=invite.FullName,Email=invite.Email,PasswordHash=hasher.Hash(password),AcceptedTermsAt=now,AcceptedPrivacyAt=now};
   if(string.IsNullOrWhiteSpace(password)||password.Length<10)return Result<UserSummaryDto>.Fail("A senha precisa ter pelo menos 10 caracteres.");
   db.Users.Add(user);
  }
  else if(!user.IsActive||user.IsBlocked)return Result<UserSummaryDto>.Fail("Não foi possível aceitar o convite com este e-mail. Fale com o suporte.");
  if(await db.AccountMembers.AnyAsync(x=>x.AccountId==account.Id&&x.UserId==user.Id&&!x.IsDeleted,ct))return Result<UserSummaryDto>.Fail("Este usuário já pertence à conta.");
  var member=new AccountMember{AccountId=account.Id,UserId=user.Id,RoleCode=invite.RoleCode};member.Join();
  db.AccountMembers.Add(member);
  invite.Use(now,user.Id);
  db.AccountModuleAuditLogs.Add(new(){AccountId=account.Id,ActorUserId=user.Id,Action="AccountInvitationAccepted",EntityType=nameof(AccountMember),EntityId=member.Id.ToString(),Summary=$"Convite aceito por {invite.Email} com perfil {invite.RoleCode}.",CorrelationId=correlationId});
  try{await db.SaveChangesAsync(ct);}catch(DbUpdateException ex){logger.LogError(ex,"ACCOUNT_INVITATION_ACCEPT_FAILED InvitationId {InvitationId} CorrelationId {CorrelationId}",invite.Id,correlationId);return Result<UserSummaryDto>.Fail("Não foi possível aceitar o convite agora. Tente novamente.");}
  await tx.CommitAsync(ct);
  logger.LogInformation("ACCOUNT_INVITATION_ACCEPTED InvitationId {InvitationId} AccountId {AccountId} CorrelationId {CorrelationId}",invite.Id,account.Id,correlationId);
  return Result<UserSummaryDto>.Ok(new UserSummaryDto(user.Id,user.Name,user.Email,user.Role.ToString(),user.Plan.ToString(),user.SessionVersion));
 }
 EmailOutboxMessage BuildOutbox(DateTime now,Guid accountId,string normalized,string link,string accountName,string roleCode,string fullName,DateTime expiresAt,Guid invitationId)
 {
  var (subject,text,html)=Template(link,accountName,roleCode,fullName,(int)Math.Ceiling((expiresAt-now).TotalHours));
  var recipientProtector=protection.CreateProtector("OrcaFacil.EmailOutbox.Recipient.v1");
  var payloadProtector=protection.CreateProtector("OrcaFacil.EmailOutbox.Payload.v1");
  var recipientHash=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secrets.Value.SecurityEventPepper),Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
  return new EmailOutboxMessage{AccountId=accountId,TemplateCode="account-invite",RecipientHash=recipientHash,RecipientMasked=Mask(normalized),ProtectedRecipient=recipientProtector.Protect(normalized),ProtectedPayload=payloadProtector.Protect(JsonSerializer.Serialize(new OutboxPayload(subject,text,html,expiresAt))),Priority=EmailPriority.Normal,CorrelationId=$"account-invite:{invitationId}",IdempotencyKey=$"account-invite:{invitationId}"};
 }
 static (string Subject,string Text,string Html) Template(string link,string accountName,string roleCode,string fullName,int validHours)
 {var role=roleCode=="Owner"?"proprietário":roleCode=="ReadOnly"?"consulta":"colaboração";var who=string.IsNullOrWhiteSpace(fullName)?string.Empty:$"Olá, {fullName}! ";
  return ("Fechô: convite para a conta "+accountName,
  $"{who}{accountName} convidou você para colaborar com perfil de {roleCode} no Fechô. Defina sua senha no link: {link}\nO link vale por cerca de {validHours}h e só pode ser usado uma vez.\nMNSOFT — CNPJ 18.160.057/0001-13",
  $"<div style=\"background:#f4f6f8;padding:32px;font-family:Arial,sans-serif;color:#172033\"><div style=\"max-width:600px;margin:auto;background:#fff\"><div style=\"background:#102a43;color:#fff;padding:24px;font-size:24px;font-weight:bold\">Fechô</div><div style=\"padding:32px\"><h1>Convite para {System.Net.WebUtility.HtmlEncode(accountName)}</h1><p>{System.Net.WebUtility.HtmlEncode($"{who}{accountName} convidou você para colaborar no Fechô com perfil de {role}.")}</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\" style=\"display:inline-block;background:#16865b;color:#fff;padding:14px 22px;text-decoration:none;border-radius:6px\">Aceitar convite</a></p><p>Este link vale por cerca de {validHours}h e só pode ser usado uma vez. Se você não esperava este convite, ignore esta mensagem.</p><p style=\"font-size:13px\">MNSOFT · CNPJ 18.160.057/0001-13 · comercial@mnsoft.com.br</p></div></div></div>");
 }
 static string Mask(string e){var a=e.IndexOf('@');return a<2?"***":$"{e[0]}***{e[(a-1)..]}";}
}
