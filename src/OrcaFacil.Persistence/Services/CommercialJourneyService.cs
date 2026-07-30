using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Services;

/// <summary>Transactional application boundary for the quote-to-cash journey.</summary>
public sealed class CommercialJourneyService(
    OrcaFacilDbContext db, ICurrentAccountService currentAccount, ICurrentUserService currentUser,
    IPlanAccessService plans, IDocumentSnapshotSerializer snapshots, IPublicDocumentTokenService tokens,
    IDocumentStatusTransitionService documentTransitions, IWorkOrderStatusTransitionService workOrderTransitions,
    INumberToWordsService numberToWords) : ICommercialJourneyService, IManualPaymentRegistrationService
{
    private string CorrelationId => Guid.NewGuid().ToString("N");
    private Guid AccountId => currentAccount.AccountId ?? throw new InvalidOperationException("Conta ativa não selecionada.");

    public async Task<RevisionResult> CreateRevisionAsync(Guid documentId, string templateCode, CancellationToken ct = default)
    {
        var correlation = CorrelationId;
        await currentAccount.EnsureAccountAccessAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await OwnedDocument(documentId, ct);
        if (document is null) return Revision(false, "NotFound", "Orçamento não encontrado.", null, null, correlation);
        if (document.Items.Count == 0) return Revision(false, "NoItems", "Inclua ao menos um serviço.", document.Id, document.Status, correlation);
        document.CalculateTotals();
        var issuer = await db.IssuerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == document.UserId, ct);
        var client = document.ClientId is Guid clientId
            ? await db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clientId && x.AccountId == AccountId, ct) : null;
        var value = new DocumentSnapshot(
            new(issuer?.BusinessName ?? string.Empty, issuer?.DocumentNumber, issuer?.Email, issuer?.Phone, issuer?.Address, issuer?.City, null, issuer?.LogoPath, issuer?.PixKey, null),
            new(document.ClientName, client?.PersonType.ToString(), document.ClientDocument, document.ClientPhone, document.ClientEmail, client?.Address, document.ClientCity, null),
            new(document.Number, document.IssueDate, document.ValidUntil, null, null, null, document.Notes, templateCode, null, null, true, document.Subtotal, document.Discount, document.Total),
            document.Items.Select(x => new QuoteItemSnapshot(x.Description, null, x.Quantity, x.UnitPrice, x.Discount, x.Quantity * x.UnitPrice, x.CalculateTotal())).ToArray());
        var serialized = snapshots.Serialize(value);
        await db.DocumentRevisions.Where(x => x.AccountId == AccountId && x.DocumentId == document.Id && x.IsCurrent)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false).SetProperty(x => x.Status, DocumentRevisionStatus.Superseded), ct);
        var next = (await db.DocumentRevisions.Where(x => x.AccountId == AccountId && x.DocumentId == document.Id)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;
        var revision = new DocumentRevision { AccountId = AccountId, DocumentId = document.Id, VersionNumber = next,
            CreatedByUserId = currentUser.UserId, SnapshotHash = serialized.Hash, ProtectedSnapshot = serialized.Json,
            TemplateCode = string.IsNullOrWhiteSpace(templateCode) ? "essential" : templateCode, Total = document.Total,
            ValidUntil = document.ValidUntil, IsCurrent = true };
        db.DocumentRevisions.Add(revision);
        AddEvent("QuoteRevisionCreated", document.Id, $"Versão {next} criada.");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Revision(true, "Created", $"Versão {next} criada.", revision.Id, revision.Status.ToString(), correlation);
    }

    public async Task<PublicQuoteResult> CreatePublicAccessAsync(Guid documentId, TimeSpan validity, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var accessDecision = await plans.CanUseAsync(AccountId, "public_link.enabled", ct);
        if (!accessDecision.IsAllowed) return new(false, accessDecision.InternalReason, accessDecision.UserMessage, null, null, correlation, "ComparePlans", "/Subscription");
        var revisionResult = await CreateRevisionAsync(documentId, "essential", ct);
        if (!revisionResult.Succeeded) return new(false, revisionResult.Code, revisionResult.Message, revisionResult.EntityId, revisionResult.CurrentStatus, correlation, null, revisionResult.RedirectPage);
        var document = await OwnedDocument(documentId, ct); var generated = tokens.Create(); var now = DateTime.UtcNow;
        var access = new PublicDocumentAccess { AccountId = AccountId, DocumentId = documentId, DocumentRevisionId = revisionResult.EntityId!.Value,
            TokenHash = generated.Hash, ExpiresAt = now.Add(validity <= TimeSpan.Zero ? TimeSpan.FromDays(30) : validity), CreatedByUserId = currentUser.UserId };
        db.PublicDocumentAccesses.Add(access);
        if (document is not null) { Transition(document, DocumentStatus.Sent); }
        AddEvent("QuoteSent", documentId, "Orçamento enviado."); await db.SaveChangesAsync(ct);
        return new(true, "Created", "Link seguro criado.", access.Id, document?.Status, correlation, "Share", "/Documents/Versions", generated.Token);
    }

    public async Task<PublicDecisionResult> DecideAsync(string token, PublicDocumentDecisionType decision, string customerName, string? reason,
        string? comment, string idempotencyKey, string ip, string userAgent, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var hash = tokens.Hash(token); var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var access = await db.PublicDocumentAccesses.SingleOrDefaultAsync(x => x.TokenHash == hash && !x.IsDeleted, ct);
        if (access is null || access.RevokedAt is not null || access.Status != PublicAccessStatus.Active || access.ExpiresAt <= now)
            return Decision(false, "Unavailable", "Este link expirou ou foi revogado.", null, null, correlation);
        var existing = await db.PublicDocumentDecisions.SingleOrDefaultAsync(x => x.AccountId == access.AccountId && x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return Decision(true, "IdempotentReplay", "Resposta já registrada.", existing.Id, existing.Decision.ToString(), correlation);
        if (await db.PublicDocumentDecisions.AnyAsync(x => x.AccountId == access.AccountId && x.DocumentRevisionId == access.DocumentRevisionId, ct))
            return Decision(false, "AlreadyDecided", "Esta versão já recebeu uma resposta.", null, null, correlation);
        var current = await db.DocumentRevisions.AnyAsync(x => x.Id == access.DocumentRevisionId && x.IsCurrent, ct);
        if (!current) return Decision(false, "Superseded", "Existe uma versão mais recente deste orçamento.", null, null, correlation);
        var entity = new PublicDocumentDecision { AccountId = access.AccountId, DocumentId = access.DocumentId, DocumentRevisionId = access.DocumentRevisionId,
            Decision = decision, CustomerName = Clean(customerName, 180), ReasonCode = Clean(reason, 40), Comment = Clean(comment, 1000),
            IpHash = Hash(ip), UserAgentHash = Hash(userAgent), IdempotencyKey = idempotencyKey };
        db.PublicDocumentDecisions.Add(entity);
        var document = await db.Documents.SingleAsync(x => x.Id == access.DocumentId && x.AccountId == access.AccountId, ct);
        var status = decision switch { PublicDocumentDecisionType.Approved => DocumentStatus.Approved, PublicDocumentDecisionType.Rejected => DocumentStatus.Rejected, _ => DocumentStatus.InNegotiation };
        Transition(document, status); AddEvent($"Quote{decision}", document.Id, decision == PublicDocumentDecisionType.ChangeRequested ? "Alteração solicitada." : $"Orçamento {decision}.", access.AccountId);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Decision(true, "Registered", "Resposta registrada com segurança.", entity.Id, status.ToString(), correlation);
    }

    public async Task<WorkOrderResult> ConvertToWorkOrderAsync(Guid documentId, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var allowed = await plans.CanUseAsync(AccountId, "work_orders.enabled", ct);
        if (!allowed.IsAllowed) return Work(false, allowed.InternalReason, allowed.UserMessage, null, null, correlation);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await OwnedDocument(documentId, ct);
        if (document is null || !Enum.TryParse<DocumentStatus>(document.Status, out var status) || status != DocumentStatus.Approved)
            return Work(false, "NotApproved", "Apenas orçamento aprovado pode virar ordem.", document?.Id, document?.Status, correlation);
        var revision = await db.DocumentRevisions.SingleAsync(x => x.AccountId == AccountId && x.DocumentId == documentId && x.IsCurrent, ct);
        var existing = await db.WorkOrders.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.SourceRevisionId == revision.Id, ct);
        if (existing is not null) return Work(true, "IdempotentReplay", "Ordem já criada.", existing.Id, existing.Status.ToString(), correlation);
        var order = new WorkOrder { AccountId = AccountId, SourceDocumentId = document.Id, SourceRevisionId = revision.Id,
            ClientId = document.ClientId ?? Guid.Empty, Number = $"OS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20], Title = $"Serviço do orçamento {document.Number}",
            ClientSnapshot = JsonSerializer.Serialize(new { document.ClientName, document.ClientEmail, document.ClientPhone }),
            ItemsSnapshot = JsonSerializer.Serialize(document.Items.Select(x => new { x.Description, x.Quantity, x.UnitPrice, x.Discount })),
            TotalSnapshot = revision.Total, Notes = document.Notes, CreatedByUserId = currentUser.UserId };
        db.WorkOrders.Add(order); Transition(document, DocumentStatus.ConvertedToWorkOrder); AddEvent("WorkOrderCreated", order.Id, "Ordem de serviço criada.");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Work(true, "Created", "Ordem de serviço criada.", order.Id, order.Status.ToString(), correlation);
    }

    public Task<WorkOrderResult> ScheduleAsync(Guid id, DateTime start, DateTime end, Guid? assignee, CancellationToken ct = default) =>
        ChangeOrder(id, WorkOrderStatus.Scheduled, x => { if (end <= start) return false; x.ScheduledStart = start.ToUniversalTime(); x.ScheduledEnd = end.ToUniversalTime(); x.AssignedUserId = assignee; return true; }, "Serviço agendado.", ct);
    public Task<WorkOrderResult> StartAsync(Guid id, CancellationToken ct = default) => ChangeOrder(id, WorkOrderStatus.InProgress, x => { x.StartedAt = DateTime.UtcNow; return true; }, "Execução iniciada.", ct);
    public Task<WorkOrderResult> CompleteAsync(Guid id, string? notes, CancellationToken ct = default) => ChangeOrder(id, WorkOrderStatus.Completed, x => { x.CompletedAt = DateTime.UtcNow; x.Notes = Clean(notes, 4000); return true; }, "Serviço concluído. Registre o pagamento separadamente.", ct);

    public async Task<PaymentRegistrationResult> RegisterAsync(ManualPaymentRequest request, CancellationToken ct = default)
    {
        var correlation = CorrelationId; if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey)) return Pay(false, "Invalid", "Informe valor e chave de idempotência válidos.", null, null, correlation);
        var order = await db.WorkOrders.SingleOrDefaultAsync(x => x.Id == request.WorkOrderId && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (order is null) return Pay(false, "NotFound", "Ordem não encontrada.", null, null, correlation);
        var existing = await db.ManualPayments.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null) return Pay(true, "IdempotentReplay", "Pagamento já registrado.", existing.Id, "Registered", correlation);
        var payment = new ManualPayment { AccountId = AccountId, WorkOrderId = order.Id, DocumentId = order.SourceDocumentId, ClientId = order.ClientId,
            Amount = request.Amount, PaymentMethod = Clean(request.PaymentMethod, 40) ?? "Outro", PaidAt = request.PaidAt.ToUniversalTime(), Notes = Clean(request.Notes, 1000), RegisteredByUserId = currentUser.UserId, IdempotencyKey = request.IdempotencyKey };
        db.ManualPayments.Add(payment); order.PaymentReceived = true; order.PaymentMethod = payment.PaymentMethod; AddEvent("PaymentRegistered", order.Id, "Pagamento registrado manualmente.");
        await db.SaveChangesAsync(ct); return Pay(true, "Registered", "Pagamento registrado manualmente.", payment.Id, "Registered", correlation);
    }

    public async Task<ReceiptGenerationResult> GenerateReceiptAsync(Guid paymentId, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var payment = await db.ManualPayments.SingleOrDefaultAsync(x => x.Id == paymentId && x.AccountId == AccountId, ct);
        if (payment is null) return ReceiptResult(false, "NotFound", "Pagamento não encontrado.", null, null, correlation);
        var existing = await db.Receipts.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.PaymentId == paymentId, ct);
        if (existing is not null) return ReceiptResult(true, "IdempotentReplay", "Recibo já emitido.", existing.Id, "Issued", correlation);
        var order = await db.WorkOrders.SingleAsync(x => x.Id == payment.WorkOrderId && x.AccountId == AccountId, ct);
        var issuer = await db.IssuerProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        var receipt = new Receipt { AccountId = AccountId, PaymentId = payment.Id, WorkOrderId = order.Id, ClientId = payment.ClientId,
            Number = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21], IssuerSnapshot = JsonSerializer.Serialize(issuer), ClientSnapshot = order.ClientSnapshot,
            ServiceSnapshot = order.ItemsSnapshot, Amount = payment.Amount, AmountInWords = numberToWords.ToCurrencyWords(payment.Amount), PaymentMethod = payment.PaymentMethod,
            IssuedAt = DateTime.UtcNow, City = issuer?.City, Notes = payment.Notes };
        db.Receipts.Add(receipt); AddEvent("ReceiptGenerated", receipt.Id, "Recibo gerado."); await db.SaveChangesAsync(ct);
        return ReceiptResult(true, "Issued", "Recibo gerado.", receipt.Id, "Issued", correlation);
    }

    private async Task<WorkOrderResult> ChangeOrder(Guid id, WorkOrderStatus next, Func<WorkOrder, bool> mutate, string message, CancellationToken ct)
    {
        var correlation = CorrelationId; var order = await db.WorkOrders.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (order is null) return Work(false, "NotFound", "Ordem não encontrada.", null, null, correlation);
        if (!workOrderTransitions.CanTransition(order.Status, next)) return Work(false, "InvalidTransition", "Mudança de status não permitida.", order.Id, order.Status.ToString(), correlation);
        if (!mutate(order)) return Work(false, "Invalid", "Revise os dados informados.", order.Id, order.Status.ToString(), correlation);
        order.Status = next; AddEvent($"WorkOrder{next}", order.Id, message); await db.SaveChangesAsync(ct); return Work(true, "Updated", message, order.Id, next.ToString(), correlation);
    }

    private Task<Document?> OwnedDocument(Guid id, CancellationToken ct) => db.Documents.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId && !x.IsDeleted, ct);
    private void Transition(Document document, DocumentStatus next) { var current = Enum.TryParse<DocumentStatus>(document.Status, out var value) ? value : DocumentStatus.Draft; documentTransitions.EnsureCanTransition(current, next); document.Status = next.ToString(); }
    private void AddEvent(string action, Guid entityId, string summary, Guid? account = null) => db.ActivityEvents.Add(new ActivityEvent { AccountId = account ?? AccountId, ActorUserId = currentUser.TryGetUserId(), Action = action, EntityType = "CommercialJourney", EntityId = entityId, Summary = summary });
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static RevisionResult Revision(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"Continue":"Review",ok?"/Documents/Versions":null);
    private static PublicDecisionResult Decision(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,null,null);
    private static WorkOrderResult Work(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"OpenWorkOrder":"Review",ok?"/WorkOrders/Details":null);
    private static PaymentRegistrationResult Pay(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"GenerateReceipt":"Review",ok?"/Payments/Register":null);
    private static ReceiptGenerationResult ReceiptResult(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"Download":"Review",ok?"/Receipts/Details":null);
}
