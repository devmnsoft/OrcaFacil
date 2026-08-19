using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Commercial;
using OrcaFacil.Application.Common;
using OrcaFacil.Application.Documents;
using OrcaFacil.Application.Plans;
using OrcaFacil.Application.WorkOrders;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

using OrcaFacil.Domain.Plans;
namespace OrcaFacil.Persistence.Services;

/// <summary>Transactional application boundary for the quote-to-cash journey.</summary>
public sealed class CommercialJourneyService(
    OrcaFacilDbContext db, ICurrentAccountService currentAccount, ICurrentUserService currentUser,
    IPlanAccessService plans, IDocumentSnapshotSerializer snapshots, IPublicDocumentTokenService tokens,
    IDocumentStatusTransitionService documentTransitions, IWorkOrderStatusTransitionService workOrderTransitions,
    INumberToWordsService numberToWords, ITechnicalFingerprintService fingerprints) : ICommercialJourneyService, IManualPaymentRegistrationService, IPublicDocumentAccessService
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

    public async Task<PublicQuoteResult> CreatePublicAccessAsync(Guid documentId, TimeSpan validity, CancellationToken ct = default)
    {
        var correlation = CorrelationId;
        await currentAccount.EnsureAccountAccessAsync(ct);
        var plan = await plans.CanUseAsync(AccountId, PlanFeatureCodes.PublicApprovalEnabled, ct);
        if (!plan.IsAllowed)
            return PublicQuote(false, QuoteLifecycleCode.PlanLimitReached, plan.UserMessage, correlation);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var revision = await CreateOrReuseRevisionCoreAsync(documentId, "essential", correlation, ct);
        if (!revision.Succeeded)
            return PublicQuote(false, revision.Code, revision.Message, correlation, revision.DocumentId,
                revision.RevisionId, null, revision.CurrentStatus);

        var existing = await db.PublicDocumentAccesses.AnyAsync(x => x.AccountId == AccountId
            && x.DocumentRevisionId == revision.RevisionId && x.Status == PublicAccessStatus.Active
            && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        if (existing)
            return PublicQuote(false, QuoteLifecycleCode.PublicLinkUnavailable,
                "Já existe um link ativo. Revogue-o antes de gerar outro.", correlation,
                revision.DocumentId, revision.RevisionId, null, revision.CurrentStatus);

        var document = await OwnedDocument(documentId, ct);
        if (document is null)
            return PublicQuote(false, QuoteLifecycleCode.DocumentNotFound, "Orçamento não encontrado.", correlation);
        if (!ParseDocumentStatus(document, out _))
            return PublicQuote(false, QuoteLifecycleCode.InvalidStatus, "O orçamento possui status inválido.", correlation, document.Id);

        var generated = tokens.Create();
        var access = new PublicDocumentAccess {
            AccountId = AccountId, DocumentId = document.Id, DocumentRevisionId = revision.RevisionId!.Value,
            TokenHash = generated.Hash, ExpiresAt = DateTime.UtcNow.Add(validity <= TimeSpan.Zero ? TimeSpan.FromDays(30) : validity),
            CreatedByUserId = currentUser.UserId
        };
        db.PublicDocumentAccesses.Add(access);
        SetDocumentStatus(document, DocumentStatus.Sent);
        AddEvent("QuoteSent", document.Id, "Orçamento enviado.");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return PublicQuote(true, QuoteLifecycleCode.None, "Link seguro criado.", correlation,
            document.Id, revision.RevisionId, access.Id, DocumentStatus.Sent, generated.Token);
    }

    public async Task<PublicDecisionResult> DecideAsync(string token, PublicDocumentDecisionType decision, string customerName,
        string? customerContact, string? reason, string? comment, DateTime? desiredDate, bool acceptedTerms,
        string idempotencyKey, string ip, string userAgent, CancellationToken ct = default)
    {
        var correlation = CorrelationId;
        var hash = tokens.Hash(token);
        var now = DateTime.UtcNow;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var access = await db.PublicDocumentAccesses.SingleOrDefaultAsync(x => x.TokenHash == hash && !x.IsDeleted, ct);
        if (access is null)
            return PublicDecision(false, QuoteLifecycleCode.PublicLinkUnavailable, "Link inválido.", correlation);
        if (access.RevokedAt is not null || access.Status != PublicAccessStatus.Active)
            return PublicDecision(false, QuoteLifecycleCode.PublicLinkRevoked, "Este link foi revogado.", correlation, access);
        if (access.ExpiresAt <= now)
            return PublicDecision(false, QuoteLifecycleCode.PublicLinkExpired, "Este link expirou.", correlation, access);
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerContact))
            return PublicDecision(false, QuoteLifecycleCode.InvalidStatus, "Informe o responsável e um contato para retorno.", correlation, access);
        if (decision == PublicDocumentDecisionType.Approved && !acceptedTerms)
            return PublicDecision(false, QuoteLifecycleCode.InvalidStatus, "Confirme a ciência das condições para aprovar.", correlation, access);
        if (decision == PublicDocumentDecisionType.Rejected && string.IsNullOrWhiteSpace(reason))
            return PublicDecision(false, QuoteLifecycleCode.InvalidStatus, "Informe o motivo da recusa.", correlation, access);
        if (decision == PublicDocumentDecisionType.ChangeRequested && string.IsNullOrWhiteSpace(comment))
            return PublicDecision(false, QuoteLifecycleCode.InvalidStatus, "Descreva o que deseja alterar.", correlation, access);

        var existing = await db.PublicDocumentDecisions.SingleOrDefaultAsync(
            x => x.AccountId == access.AccountId && x.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
            return PublicDecision(true, QuoteLifecycleCode.None, "Resposta já registrada.", correlation, access,
                DecisionStatus(existing.Decision), existing.Id);
        if (await db.PublicDocumentDecisions.AnyAsync(x => x.AccountId == access.AccountId && x.DocumentRevisionId == access.DocumentRevisionId, ct))
            return PublicDecision(false, QuoteLifecycleCode.DecisionAlreadyRegistered, "Esta versão já recebeu uma resposta.", correlation, access);
        if (!await db.DocumentRevisions.AnyAsync(x => x.Id == access.DocumentRevisionId && x.IsCurrent, ct))
            return PublicDecision(false, QuoteLifecycleCode.VersionOutdated, "Existe uma versão mais recente deste orçamento.", correlation, access);

        var entity = new PublicDocumentDecision {
            AccountId = access.AccountId, DocumentId = access.DocumentId, DocumentRevisionId = access.DocumentRevisionId,
            Decision = decision, CustomerName = Clean(customerName, 180), CustomerContact = Clean(customerContact, 254),
            ReasonCode = Clean(reason, 40), Comment = Clean(comment, 1000), DesiredDate = desiredDate, AcceptedTerms = acceptedTerms,
            IpHash = fingerprints.Create(ip), UserAgentHash = fingerprints.Create(userAgent), IdempotencyKey = idempotencyKey
        };
        db.PublicDocumentDecisions.Add(entity);
        var document = await db.Documents.SingleAsync(x => x.Id == access.DocumentId && x.AccountId == access.AccountId, ct);
        var status = DecisionStatus(decision);
        if (!ParseDocumentStatus(document, out _))
            return PublicDecision(false, QuoteLifecycleCode.InvalidStatus, "O orçamento possui status inválido.", correlation, access);
        SetDocumentStatus(document, status);
        AddEvent($"Quote{decision}", document.Id,
            decision == PublicDocumentDecisionType.ChangeRequested ? "Alteração solicitada." : $"Orçamento {decision}.", access.AccountId);
        db.Notifications.Add(new Notification {
            AccountId = access.AccountId, UserId = access.CreatedByUserId, DocumentId = document.Id,
            Title = decision switch { PublicDocumentDecisionType.Approved => "Proposta aprovada", PublicDocumentDecisionType.Rejected => "Proposta recusada", _ => "Alteração solicitada" },
            Message = $"{Clean(customerName, 180)} respondeu à proposta {document.Number}.",
            Type = decision == PublicDocumentDecisionType.Approved ? NotificationType.Success : NotificationType.Warning,
            Category = NotificationCategory.Document, ActionUrl = $"/Documents/Details/{document.Id}", ActionText = "Abrir proposta"
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return PublicDecision(true, QuoteLifecycleCode.None, "Resposta registrada com segurança.", correlation, access, status, entity.Id);
    }

    public async Task<OperationResult<PublicQuoteView>> OpenAsync(string token, string remoteAddress, string userAgent, CancellationToken ct = default)
    {
        var correlation = CorrelationId;
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return OperationResult<PublicQuoteView>.Failure("PublicLinkUnavailable", $"Link inválido. Referência: {correlation}.");

        var hash = tokens.Hash(token);
        var now = DateTime.UtcNow;
        var access = await db.PublicDocumentAccesses.SingleOrDefaultAsync(x => x.TokenHash == hash && !x.IsDeleted, ct);
        if (access is null)
            return OperationResult<PublicQuoteView>.Failure("PublicLinkUnavailable", $"Link inválido. Referência: {correlation}.");
        if (access.RevokedAt is not null || access.Status != PublicAccessStatus.Active)
            return OperationResult<PublicQuoteView>.Failure("PublicLinkRevoked", "Este link foi revogado.");
        if (access.ExpiresAt <= now)
            return OperationResult<PublicQuoteView>.Failure("PublicLinkExpired", "Este link expirou.");

        var revision = await db.DocumentRevisions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == access.DocumentRevisionId && x.AccountId == access.AccountId && x.DocumentId == access.DocumentId, ct);
        if (revision is null || !revision.IsCurrent)
            return OperationResult<PublicQuoteView>.Failure("VersionOutdated", "Existe uma versão mais recente deste orçamento.");

        DocumentSnapshot? snapshot;
        try { snapshot = JsonSerializer.Deserialize<DocumentSnapshot>(revision.ProtectedSnapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { snapshot = null; }
        if (snapshot is null)
            return OperationResult<PublicQuoteView>.Failure("InvalidSnapshot", $"Não foi possível abrir este orçamento. Referência: {correlation}.");

        var firstView = access.LastViewedAt is null;
        access.LastViewedAt = now;
        access.ViewCount++;
        if (await db.Documents.SingleOrDefaultAsync(x => x.Id == access.DocumentId && x.AccountId == access.AccountId && !x.IsDeleted, ct) is { } document
            && ParseDocumentStatus(document, out var status) && status == DocumentStatus.Sent)
            SetDocumentStatus(document, DocumentStatus.Viewed);
        if (firstView) {
            db.ActivityEvents.Add(new ActivityEvent { AccountId = access.AccountId, ActorUserId = null, Action = "QuoteViewed",
                EntityType = "CommercialJourney", EntityId = access.DocumentId, Summary = "Orçamento visualizado pelo link seguro." });
            db.Notifications.Add(new Notification { AccountId = access.AccountId, UserId = access.CreatedByUserId,
                DocumentId = access.DocumentId, Title = "Proposta visualizada", Message = "O cliente abriu a proposta pela primeira vez.",
                Type = NotificationType.Info, Category = NotificationCategory.Document,
                ActionUrl = $"/Documents/Details/{access.DocumentId}", ActionText = "Acompanhar proposta" });
        }
        await db.SaveChangesAsync(ct);

        var decided = await db.PublicDocumentDecisions.AsNoTracking().AnyAsync(x =>
            x.AccountId == access.AccountId && x.DocumentRevisionId == access.DocumentRevisionId, ct);
        return OperationResult<PublicQuoteView>.Success(new(access.Id, access.DocumentId, revision.VersionNumber,
            access.ExpiresAt, snapshot, decided), "Orçamento carregado.");
    }

    private async Task<RevisionResult> CreateOrReuseRevisionCoreAsync(Guid documentId, string templateCode, string correlation, CancellationToken ct)
    {
        var document = await OwnedDocument(documentId, ct);
        if (document is null)
            return Revision(false, QuoteLifecycleCode.DocumentNotFound, "Orçamento não encontrado.", correlation);
        if (!ParseDocumentStatus(document, out var status)) {
            AddEvent("QuoteInvalidStatus", document.Id, "Status inválido detectado; orçamento não alterado.");
            return Revision(false, QuoteLifecycleCode.InvalidStatus, "O orçamento possui status inválido.", correlation, document.Id);
        }
        if (document.Items.Count == 0)
            return Revision(false, QuoteLifecycleCode.NoItems, "Inclua ao menos um serviço.", correlation, document.Id, status);
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
        var current = await db.DocumentRevisions.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.DocumentId == document.Id && x.IsCurrent, ct);
        if (current?.SnapshotHash == serialized.Hash)
            return Revision(true, QuoteLifecycleCode.None, $"Versão {current.VersionNumber} reutilizada.", correlation,
                document.Id, status, current.Id, current.VersionNumber, current.SnapshotHash, true);
        if (current is not null) { current.IsCurrent = false; current.Status = DocumentRevisionStatus.Superseded; }
        var next = (await db.DocumentRevisions.Where(x => x.AccountId == AccountId && x.DocumentId == document.Id)
            .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;
        var revision = new DocumentRevision { AccountId = AccountId, DocumentId = document.Id, VersionNumber = next,
            CreatedByUserId = currentUser.UserId, SnapshotHash = serialized.Hash, ProtectedSnapshot = serialized.Json,
            TemplateCode = string.IsNullOrWhiteSpace(templateCode) ? "essential" : templateCode, Total = document.Total,
            ValidUntil = document.ValidUntil, IsCurrent = true };
        db.DocumentRevisions.Add(revision);
        AddEvent("QuoteRevisionCreated", document.Id, $"Versão {next} criada.");
        return Revision(true, QuoteLifecycleCode.None, $"Versão {next} criada.", correlation,
            document.Id, status, revision.Id, next, serialized.Hash, false);
    }

    public async Task<WorkOrderResult> ConvertToWorkOrderAsync(Guid documentId, CancellationToken ct = default)
    {
        var correlation = CorrelationId; var allowed = await plans.CanUseAsync(AccountId, PlanFeatureCodes.WorkOrdersEnabled, ct);
        if (!allowed.IsAllowed) return Work(false, allowed.InternalReason, allowed.UserMessage, null, null, correlation);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await OwnedDocument(documentId, ct);
        if (document is null || !Enum.TryParse<DocumentStatus>(document.Status, out var status) || status != DocumentStatus.Approved)
            return Work(false, "NotApproved", "Apenas orçamento aprovado pode virar ordem.", document?.Id, document?.Status, correlation);
        if (document.ClientId is not { } clientId || !await db.Clients.AnyAsync(x => x.AccountId == AccountId && x.Id == clientId && !x.IsDeleted, ct))
            return Work(false, "InvalidClient", "Vincule um cliente válido da conta antes de gerar a OS.", document.Id, document.Status, correlation);
        var revision = await db.DocumentRevisions.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.DocumentId == documentId && x.IsCurrent, ct);
        if (revision is null) return Work(false, "RevisionRequired", "Gere a versão aprovada da proposta antes de criar a OS.", document.Id, document.Status, correlation);
        var existing = await db.WorkOrders.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.SourceDocumentId == document.Id && !x.IsDeleted, ct);
        if (existing is not null) return Work(true, "IdempotentReplay", "Ordem já criada.", existing.Id, existing.Status.ToString(), correlation);
        var order = new WorkOrder { AccountId = AccountId, SourceDocumentId = document.Id, SourceRevisionId = revision.Id,
            ClientId = clientId, Number = $"OS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20], Title = $"Serviço do orçamento {document.Number}",
            ClientSnapshot = JsonSerializer.Serialize(new { document.ClientName, document.ClientEmail, document.ClientPhone }),
            ItemsSnapshot = JsonSerializer.Serialize(document.Items.Select(x => new { x.Description, x.Quantity, x.UnitPrice, x.Discount })),
            TotalSnapshot = revision.Total, Notes = document.Notes, CreatedByUserId = currentUser.UserId };
        db.WorkOrders.Add(order);
        var checklist = new[] { "Confirmar dados do cliente", "Preparar material", "Executar serviço", "Validar entrega", "Finalizar atendimento" };
        db.WorkOrderChecklistItems.AddRange(checklist.Select((description, position) => new WorkOrderChecklistItem
        {
            AccountId = AccountId, WorkOrderId = order.Id, Description = description, Position = position + 1
        }));
        Transition(document, DocumentStatus.ConvertedToWorkOrder); AddEvent("WorkOrderCreated", order.Id, "Ordem de serviço criada com checklist operacional.");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Work(true, "Created", "Ordem de serviço criada.", order.Id, order.Status.ToString(), correlation);
    }

    public Task<WorkOrderResult> ScheduleAsync(Guid id, DateTime start, DateTime end, Guid? assignee, CancellationToken ct = default) =>
        ChangeOrder(id, WorkOrderStatus.Scheduled, x => { if (end <= start) return false; x.ScheduledStart = start.ToUniversalTime(); x.ScheduledEnd = end.ToUniversalTime(); x.AssignedUserId = assignee; return true; }, "Serviço agendado.", ct);
    public Task<WorkOrderResult> StartAsync(Guid id, CancellationToken ct = default) => ChangeOrder(id, WorkOrderStatus.InProgress, x => { x.StartedAt = DateTime.UtcNow; return true; }, "Execução iniciada.", ct);
    public Task<WorkOrderResult> PauseAsync(Guid id, CancellationToken ct = default) => ChangeOrder(id, WorkOrderStatus.Paused, _ => true, "Execução pausada.", ct);
    public Task<WorkOrderResult> ResumeAsync(Guid id, CancellationToken ct = default) => ChangeOrder(id, WorkOrderStatus.InProgress, _ => true, "Execução retomada.", ct);
    public Task<WorkOrderResult> CompleteAsync(Guid id, string? notes, CancellationToken ct = default) =>
        ChangeOrder(id, WorkOrderStatus.Completed, x => { x.CompletedAt = DateTime.UtcNow; if (!string.IsNullOrWhiteSpace(notes)) x.Notes = Clean(notes, 4000); return true; }, "Serviço concluído. Pendência financeira registrada quando aplicável.", ct);
    public Task<WorkOrderResult> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var cleanReason = Clean(reason, 1000);
        if (cleanReason is null) return Task.FromResult(Work(false, "CancellationReasonRequired", "Informe o motivo do cancelamento.", id, null, CorrelationId));
        return ChangeOrder(id, WorkOrderStatus.Cancelled, x => { x.CancelledAt = DateTime.UtcNow; x.CancellationReason = cleanReason; return true; }, $"OS cancelada. Motivo: {cleanReason}", ct);
    }

    public async Task<PaymentRegistrationResult> RegisterAsync(ManualPaymentRequest request, CancellationToken ct = default)
    {
        var correlation = CorrelationId;
        await currentAccount.EnsureAccountAccessAsync(ct);
        var allowed = await plans.CanUseAsync(AccountId, PlanFeatureCodes.ManualPaymentsEnabled, ct);
        if (!allowed.IsAllowed) return Pay(false, allowed.InternalReason, allowed.UserMessage, null, null, correlation);
        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return Pay(false, "Invalid", "Informe valor e chave de idempotência válidos.", null, null, correlation);
        if (!PaymentMethodCodes.TryParse(request.PaymentMethod, out var method))
            return Pay(false, "InvalidPaymentMethod", "Escolha uma forma de pagamento válida.", null, null, correlation);
        if (request.PaidAt.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5))
            return Pay(false, "InvalidPaidAt", "A data do recebimento não pode estar no futuro.", null, null, correlation);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.WorkOrders.SingleOrDefaultAsync(x => x.Id == request.WorkOrderId && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (order is null) return Pay(false, "NotFound", "Ordem não encontrada.", null, null, correlation);
        var existing = await db.ManualPayments.SingleOrDefaultAsync(x => x.AccountId == AccountId && x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null) return Pay(true, "IdempotentReplay", "Pagamento já registrado.", existing.Id, "Registered", correlation);
        var paid = await db.ManualPayments.Where(x => x.AccountId == AccountId && x.WorkOrderId == order.Id && !x.IsDeleted && x.Status == FinancialRecordStatus.Active)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var balance = Math.Max(0m, order.TotalSnapshot - paid);
        if (balance == 0m) return Pay(false, "AlreadyPaid", "Esta ordem já está totalmente paga.", null, "Paid", correlation);
        if (request.Amount > balance) return Pay(false, "AmountExceedsBalance", $"O valor informado supera o saldo de {balance:C}.", null, "Partial", correlation);
        var payment = new ManualPayment { AccountId = AccountId, WorkOrderId = order.Id, DocumentId = order.SourceDocumentId, ClientId = order.ClientId,
            Amount = request.Amount, PaymentMethod = method.ToCode(), PaidAt = request.PaidAt.ToUniversalTime(), Notes = Clean(request.Notes, 1000), RegisteredByUserId = currentUser.UserId, IdempotencyKey = request.IdempotencyKey };
        db.ManualPayments.Add(payment);
        var remaining = balance - request.Amount;
        order.PaymentReceived = remaining == 0m; order.PaymentMethod = payment.PaymentMethod;
        AddEvent("PaymentRegistered", order.Id, remaining == 0m ? "Pagamento total registrado manualmente." : "Pagamento parcial registrado manualmente.");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Pay(true, remaining == 0m ? "Registered" : "PartiallyPaid", "Pagamento registrado manualmente.", payment.Id, remaining == 0m ? "Paid" : "Partial", correlation);
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

    public Task<CommercialResult> ScheduleFollowUpAsync(FollowUpRequest request, CancellationToken ct = default) =>
        SaveFollowUpAsync(request, FollowUpStatus.Scheduled, "FOLLOW_UP_SCHEDULED", "Retorno agendado.", ct);

    public Task<CommercialResult> SnoozeFollowUpAsync(FollowUpRequest request, CancellationToken ct = default) =>
        SaveFollowUpAsync(request, FollowUpStatus.Snoozed, "FOLLOW_UP_SNOOZED", "Retorno adiado.", ct);

    public async Task<CommercialResult> CompleteFollowUpAsync(Guid documentId, string? note, CancellationToken ct = default)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        var document = await OwnedDocument(documentId, ct);
        if (document is null) return FollowUpResult(false, "NotFound", "Orçamento não encontrado.", null);
        document.LastFollowUpAt = DateTime.UtcNow;
        document.NextFollowUpAt = null;
        document.FollowUpStatus = FollowUpStatus.Completed;
        document.FollowUpNote = Clean(note, 1000);
        document.Touch();
        AddEvent("FOLLOW_UP_COMPLETED", document.Id, document.FollowUpNote ?? "Acompanhamento concluído.");
        await db.SaveChangesAsync(ct);
        return FollowUpResult(true, "Completed", "Acompanhamento concluído.", document.Id);
    }

    private async Task<CommercialResult> SaveFollowUpAsync(FollowUpRequest request, FollowUpStatus status, string action, string message, CancellationToken ct)
    {
        await currentAccount.EnsureAccountAccessAsync(ct);
        if (request.NextFollowUpAt is null || request.NextFollowUpAt <= DateTime.UtcNow)
            return FollowUpResult(false, "InvalidDate", "Escolha uma data futura para o retorno.", request.DocumentId);
        var document = await OwnedDocument(request.DocumentId, ct);
        if (document is null) return FollowUpResult(false, "NotFound", "Orçamento não encontrado.", null);
        document.NextFollowUpAt = request.NextFollowUpAt;
        document.FollowUpStatus = status;
        document.FollowUpNote = Clean(request.Note, 1000);
        document.Touch();
        AddEvent(action, document.Id, $"{message} {request.NextFollowUpAt.Value.ToLocalTime():dd/MM/yyyy HH:mm}.");
        await db.SaveChangesAsync(ct);
        return FollowUpResult(true, status.ToString(), message, document.Id);
    }

    private async Task<WorkOrderResult> ChangeOrder(Guid id, WorkOrderStatus next, Func<WorkOrder, bool> mutate, string message, CancellationToken ct)
    {
        var correlation = CorrelationId; var order = await db.WorkOrders.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId && !x.IsDeleted, ct);
        if (order is null) return Work(false, "NotFound", "Ordem não encontrada.", null, null, correlation);
        if (!workOrderTransitions.CanTransition(order.Status, next)) return Work(false, "InvalidTransition", "Mudança de status não permitida.", order.Id, order.Status.ToString(), correlation);
        if (!mutate(order)) return Work(false, "Invalid", "Revise os dados informados.", order.Id, order.Status.ToString(), correlation);
        order.Status = next;
        if (next == WorkOrderStatus.Completed && !order.PaymentReceived && order.TotalSnapshot > 0 &&
            !await db.FinancialEntries.AnyAsync(x => x.AccountId == AccountId && x.WorkOrderId == order.Id && !x.IsDeleted, ct))
        {
            db.FinancialEntries.Add(new FinancialEntry { AccountId = AccountId, ClientId = order.ClientId,
                DocumentId = order.SourceDocumentId, WorkOrderId = order.Id, Origin = FinancialEntryOrigin.WorkOrder,
                Description = $"Serviço concluído · {order.Number}", DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = order.TotalSnapshot, Status = FinancialEntryStatus.Pending });
        }
        AddEvent($"WorkOrder{next}", order.Id, message); await db.SaveChangesAsync(ct); return Work(true, "Updated", message, order.Id, next.ToString(), correlation);
    }

    private Task<Document?> OwnedDocument(Guid id, CancellationToken ct) => db.Documents.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.AccountId == AccountId && !x.IsDeleted, ct);
    private static bool ParseDocumentStatus(Document document, out DocumentStatus status) =>
        Enum.TryParse(document.Status, ignoreCase: false, out status) && Enum.IsDefined(status);
    private void SetDocumentStatus(Document document, DocumentStatus next)
    {
        if (!ParseDocumentStatus(document, out var current)) throw new InvalidOperationException("Status de orçamento inválido.");
        documentTransitions.EnsureCanTransition(current, next);
        document.Status = next.ToString();
    }
    private void Transition(Document document, DocumentStatus next) => SetDocumentStatus(document, next);
    private void AddEvent(string action, Guid entityId, string summary, Guid? account = null) => db.ActivityEvents.Add(new ActivityEvent { AccountId = account ?? AccountId, ActorUserId = currentUser.TryGetUserId(), Action = action, EntityType = "CommercialJourney", EntityId = entityId, Summary = summary });
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static RevisionResult Revision(bool ok, QuoteLifecycleCode code, string message, string correlation,
        Guid? documentId = null, DocumentStatus? status = null, Guid? revisionId = null, int? version = null,
        string? snapshotHash = null, bool reused = false) =>
        new(ok, code, message, documentId, revisionId, null, status, correlation, version, snapshotHash, reused);
    private static PublicQuoteResult PublicQuote(bool ok, QuoteLifecycleCode code, string message, string correlation,
        Guid? documentId = null, Guid? revisionId = null, Guid? accessId = null, DocumentStatus? status = null,
        string? publicToken = null) => new(ok, code, message, documentId, revisionId, accessId, status, correlation, publicToken);
    private static PublicDecisionResult PublicDecision(bool ok, QuoteLifecycleCode code, string message, string correlation,
        PublicDocumentAccess? access = null, DocumentStatus? status = null, Guid? decisionId = null) =>
        new(ok, code, message, access?.DocumentId, access?.DocumentRevisionId, access?.Id, status, correlation, decisionId);
    private static DocumentStatus DecisionStatus(PublicDocumentDecisionType decision) => decision switch
    {
        PublicDocumentDecisionType.Approved => DocumentStatus.Approved,
        PublicDocumentDecisionType.Rejected => DocumentStatus.Rejected,
        _ => DocumentStatus.InNegotiation
    };
    private static WorkOrderResult Work(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"OpenWorkOrder":"Review",ok?"/WorkOrders/Details":null);
    private static PaymentRegistrationResult Pay(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"GenerateReceipt":"Review",ok?"/Payments/Register":null);
    private static ReceiptGenerationResult ReceiptResult(bool ok,string code,string msg,Guid? id,string? status,string c)=>new(ok,code,msg,id,status,c,ok?"Download":"Review",ok?"/Receipts/Details":null);
    private CommercialResult FollowUpResult(bool ok, string code, string message, Guid? id) =>
        new(ok, code, message, id, null, CorrelationId, ok ? "OpenDocument" : "Review", ok ? "/Documents/Details" : null);
}
