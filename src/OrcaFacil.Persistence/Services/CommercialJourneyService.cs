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
    INumberToWordsService numberToWords, ITechnicalFingerprintService fingerprints) : ICommercialJourneyService, IManualPaymentRegistrationService
{
    private string CorrelationId => Guid.NewGuid().ToString("N");
    private Guid AccountId => currentAccount.AccountId ?? throw new InvalidOperationException("Conta ativa não selecionada.");

    public async Task<RevisionResult> CreateRevisionAsync(Guid documentId, string templateCode, CancellationToken ct = default)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        var correlation = CorrelationId;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await CreateOrReuseRevisionCoreAsync(documentId, templateCode, correlation, ct);
        if (!result.Succeeded) return result;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    private async Task<RevisionResult> CreateOrReuseRevisionCoreAsync(
        Guid documentId, string templateCode, string correlation, CancellationToken ct)
    {
        var document = await OwnedDocument(documentId, ct);
        if (document is null)
            return RevisionFailure(QuoteLifecycleCode.DocumentNotFound, "Orçamento não encontrado.", correlation);
        if (!ParseDocumentStatus(document, out var documentStatus))
        {
            AddEvent("QuoteInvalidStatus", document.Id, "O orçamento possui estado inválido.");
            return RevisionFailure(QuoteLifecycleCode.InvalidStatus, "O estado do orçamento é inválido.", correlation, document.Id);
        }
        if (document.Items.Count == 0)
            return RevisionFailure(QuoteLifecycleCode.NoItems, "Inclua ao menos um serviço.", correlation, document.Id, documentStatus);

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
        var current = await db.DocumentRevisions.SingleOrDefaultAsync(
            x => x.AccountId == AccountId && x.DocumentId == document.Id && x.IsCurrent, ct);
        if (current is not null && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(current.SnapshotHash), Encoding.UTF8.GetBytes(serialized.Hash)))
            return new(true, QuoteLifecycleCode.None, "Revisão atual reutilizada.", document.Id, current.Id, null,
                documentStatus, correlation, current.VersionNumber, current.SnapshotHash, true);

        if (current is not null)
        {
            current.IsCurrent = false;
            current.Status = DocumentRevisionStatus.Superseded;
        }
        var next = (await db.DocumentRevisions.Where(x => x.AccountId == AccountId && x.DocumentId == document.Id)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;
        var revision = new DocumentRevision { AccountId = AccountId, DocumentId = document.Id, VersionNumber = next,
            CreatedByUserId = currentUser.UserId, SnapshotHash = serialized.Hash, ProtectedSnapshot = serialized.Json,
            TemplateCode = string.IsNullOrWhiteSpace(templateCode) ? "essential" : templateCode, Total = document.Total,
            ValidUntil = document.ValidUntil, IsCurrent = true };
        db.DocumentRevisions.Add(revision);
        AddEvent("QuoteRevisionCreated", document.Id, $"Versão {next} criada.");
        return new(true, QuoteLifecycleCode.None, $"Versão {next} criada.", document.Id, revision.Id, null,
            documentStatus, correlation, next, serialized.Hash, false);
    }

    public async Task<PublicQuoteResult> CreatePublicAccessAsync(Guid documentId, TimeSpan validity, CancellationToken ct = default)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        var correlation = CorrelationId;
        var planDecision = await plans.CanUseAsync(AccountId, "public_link.enabled", ct);
        if (!planDecision.IsAllowed)
            return new(false, QuoteLifecycleCode.PlanLimitReached, planDecision.UserMessage, documentId, CorrelationId: correlation);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var revisionResult = await CreateOrReuseRevisionCoreAsync(documentId, "essential", correlation, ct);
        if (!revisionResult.Succeeded)
            return new(false, revisionResult.Code, revisionResult.Message, revisionResult.DocumentId,
                revisionResult.RevisionId, null, revisionResult.CurrentStatus, correlation);

        var activeAccess = await db.PublicDocumentAccesses.SingleOrDefaultAsync(x =>
            x.AccountId == AccountId && x.DocumentRevisionId == revisionResult.RevisionId && !x.IsDeleted &&
            x.Status == PublicAccessStatus.Active && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        if (activeAccess is not null)
            return new(false, QuoteLifecycleCode.PublicLinkUnavailable,
                "Já existe um link ativo. Revogue-o antes de gerar outro.", documentId,
                revisionResult.RevisionId, activeAccess.Id, revisionResult.CurrentStatus, correlation);

        var document = await OwnedDocument(documentId, ct);
        if (document is null || !ParseDocumentStatus(document, out _))
            return new(false, document is null ? QuoteLifecycleCode.DocumentNotFound : QuoteLifecycleCode.InvalidStatus,
                document is null ? "Orçamento não encontrado." : "O estado do orçamento é inválido.", documentId,
                revisionResult.RevisionId, CorrelationId: correlation);
        var generated = tokens.Create();
        var access = new PublicDocumentAccess { AccountId = AccountId, DocumentId = documentId,
            DocumentRevisionId = revisionResult.RevisionId!.Value, TokenHash = generated.Hash,
            ExpiresAt = DateTime.UtcNow.Add(validity <= TimeSpan.Zero ? TimeSpan.FromDays(30) : validity),
            CreatedByUserId = currentUser.UserId };
        db.PublicDocumentAccesses.Add(access);
        SetDocumentStatus(document, DocumentStatus.Sent);
        AddEvent("QuoteSent", documentId, "Orçamento enviado.");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, QuoteLifecycleCode.None, "Link seguro criado.", document.Id, revisionResult.RevisionId,
            access.Id, DocumentStatus.Sent, correlation, generated.Token);
    }

    public async Task<PublicDecisionResult> DecideAsync(string token, PublicDocumentDecisionType decision, string customerName, string? reason,
        string? comment, string idempotencyKey, string ip, string userAgent, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var hash = tokens.Hash(token); var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var access = await db.PublicDocumentAccesses.SingleOrDefaultAsync(x => x.TokenHash == hash && !x.IsDeleted, ct);
        if (access is null)
            return DecisionFailure(QuoteLifecycleCode.PublicLinkUnavailable, "Link inválido.", correlation);
        if (access.RevokedAt is not null || access.Status != PublicAccessStatus.Active)
            return DecisionFailure(QuoteLifecycleCode.PublicLinkRevoked, "Este link foi revogado.", correlation, access);
        if (access.ExpiresAt <= now)
            return DecisionFailure(QuoteLifecycleCode.PublicLinkExpired, "Este link expirou.", correlation, access);
        var existing = await db.PublicDocumentDecisions.SingleOrDefaultAsync(x => x.AccountId == access.AccountId && x.IdempotencyKey == idempotencyKey, ct);
        var document = await db.Documents.SingleAsync(x => x.Id == access.DocumentId && x.AccountId == access.AccountId, ct);
        if (!ParseDocumentStatus(document, out var currentStatus))
            return DecisionFailure(QuoteLifecycleCode.InvalidStatus, "O estado do orçamento é inválido.", correlation, access);
        if (existing is not null)
            return new(true, QuoteLifecycleCode.None, "Resposta já registrada.", access.DocumentId,
                access.DocumentRevisionId, access.Id, currentStatus, correlation, existing.Id);
        if (await db.PublicDocumentDecisions.AnyAsync(x => x.AccountId == access.AccountId && x.DocumentRevisionId == access.DocumentRevisionId, ct))
            return DecisionFailure(QuoteLifecycleCode.DecisionAlreadyRegistered, "Esta versão já recebeu uma resposta.", correlation, access, currentStatus);
        var current = await db.DocumentRevisions.AnyAsync(x => x.Id == access.DocumentRevisionId && x.IsCurrent, ct);
        if (!current) return DecisionFailure(QuoteLifecycleCode.VersionOutdated, "Existe uma versão mais recente deste orçamento.", correlation, access, currentStatus);
        var entity = new PublicDocumentDecision { AccountId = access.AccountId, DocumentId = access.DocumentId, DocumentRevisionId = access.DocumentRevisionId,
            Decision = decision, CustomerName = Clean(customerName, 180), ReasonCode = Clean(reason, 40), Comment = Clean(comment, 1000),
            IpHash = fingerprints.Create(ip), UserAgentHash = fingerprints.Create(userAgent), IdempotencyKey = idempotencyKey };
        db.PublicDocumentDecisions.Add(entity);
        var status = decision switch { PublicDocumentDecisionType.Approved => DocumentStatus.Approved, PublicDocumentDecisionType.Rejected => DocumentStatus.Rejected, _ => DocumentStatus.InNegotiation };
        SetDocumentStatus(document, status); AddEvent($"Quote{decision}", document.Id, decision == PublicDocumentDecisionType.ChangeRequested ? "Alteração solicitada." : $"Orçamento {decision}.", access.AccountId);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return new(true, QuoteLifecycleCode.None, "Resposta registrada com segurança.", access.DocumentId,
            access.DocumentRevisionId, access.Id, status, correlation, entity.Id);
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
    private static bool ParseDocumentStatus(Document document, out DocumentStatus status) =>
        Enum.TryParse(document.Status, ignoreCase: false, out status);
    private void SetDocumentStatus(Document document, DocumentStatus next)
    {
        if (!ParseDocumentStatus(document, out var current))
            throw new InvalidOperationException("O documento possui estado inválido.");
        documentTransitions.EnsureCanTransition(current, next);
        document.Status = next.ToString();
    }
    private void Transition(Document document, DocumentStatus next) => SetDocumentStatus(document, next);
    private void AddEvent(string action, Guid entityId, string summary, Guid? account = null) => db.ActivityEvents.Add(new ActivityEvent { AccountId = account ?? AccountId, ActorUserId = currentUser.TryGetUserId(), Action = action, EntityType = "CommercialJourney", EntityId = entityId, Summary = summary });
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static RevisionResult RevisionFailure(QuoteLifecycleCode code, string message, string correlation,
        Guid? documentId = null, DocumentStatus? status = null) =>
        new(false, code, message, documentId, CurrentStatus: status, CorrelationId: correlation);
    private static PublicDecisionResult DecisionFailure(QuoteLifecycleCode code, string message, string correlation,
        PublicDocumentAccess? access = null, DocumentStatus? status = null) =>
        new(false, code, message, access?.DocumentId, access?.DocumentRevisionId, access?.Id, status, correlation);
    private static WorkOrderResult Work(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"OpenWorkOrder":"Review",ok?"/WorkOrders/Details":null);
    private static PaymentRegistrationResult Pay(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"GenerateReceipt":"Review",ok?"/Payments/Register":null);
    private static ReceiptGenerationResult ReceiptResult(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"Download":"Review",ok?"/Receipts/Details":null);
}
