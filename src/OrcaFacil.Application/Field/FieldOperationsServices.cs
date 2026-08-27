namespace OrcaFacil.Application.Field;

public enum FieldTechnicianStatus { Available, InService, Traveling, Absent, OffHours, Inactive }
public enum FieldVisitOutcome { Completed, PendingReview, RequiresReturn }
public enum OfflineSyncState { Pending, Applied, Conflict }

public sealed record FieldTechnician(Guid AccountId, Guid UserId, FieldTechnicianStatus Status, int DailyLimit, int AssignedToday);
public sealed record FieldWorkOrder(Guid Id, Guid AccountId, Guid? ClientId, Guid? TechnicianId, Guid? TeamId,
    DateTime? ScheduledStart, DateTime? ScheduledEnd, string Address, bool RequiresChecklist, bool ChecklistComplete,
    bool RequiresEvidence, bool HasEvidence, bool RequiresSignature, bool HasSignature);
public sealed record FieldVisitSession(Guid Id, Guid AccountId, Guid WorkOrderId, Guid TechnicianId, DateTime CheckedInAt, DateTime? CheckedOutAt = null);
public sealed record OfflineQueueItem(Guid AccountId, string IdempotencyKey, string Kind, string PayloadHash, OfflineSyncState State = OfflineSyncState.Pending);

public sealed record FieldOperationResult(bool Succeeded, string? Error = null)
{
    public static FieldOperationResult Ok() => new(true);
    public static FieldOperationResult Fail(string error) => new(false, error);
}

public sealed class FieldTeamService
{
    public FieldOperationResult CanAssign(bool teamActive, FieldTechnician technician)
    {
        if (!teamActive) return FieldOperationResult.Fail("A equipe inativa não recebe novas ordens de serviço.");
        if (technician.Status is FieldTechnicianStatus.Inactive or FieldTechnicianStatus.Absent)
            return FieldOperationResult.Fail("O técnico não está disponível para atribuição.");
        if (technician.DailyLimit > 0 && technician.AssignedToday >= technician.DailyLimit)
            return FieldOperationResult.Fail("O limite diário do técnico foi atingido.");
        return FieldOperationResult.Ok();
    }
}

public sealed class FieldDispatchService
{
    public FieldOperationResult Validate(FieldWorkOrder order, Guid accountId)
    {
        if (order.AccountId != accountId) return FieldOperationResult.Fail("Ordem de serviço não pertence à conta atual.");
        if (order.ClientId is null || order.ClientId == Guid.Empty) return FieldOperationResult.Fail("Selecione um cliente antes do despacho.");
        if (order.TechnicianId is null && order.TeamId is null) return FieldOperationResult.Fail("Selecione um técnico ou uma equipe.");
        return FieldOperationResult.Ok();
    }
}

public sealed class FieldScheduleService
{
    public FieldOperationResult Validate(FieldWorkOrder order, IEnumerable<FieldWorkOrder> existing, bool conflictConfirmed = false)
    {
        if (order.ScheduledStart is null || order.ScheduledEnd is null || order.ScheduledEnd <= order.ScheduledStart)
            return FieldOperationResult.Fail("Informe uma janela de atendimento válida.");
        var conflict = existing.Any(x => x.AccountId == order.AccountId && x.TechnicianId == order.TechnicianId &&
            x.Id != order.Id && x.ScheduledStart < order.ScheduledEnd && x.ScheduledEnd > order.ScheduledStart);
        return conflict && !conflictConfirmed
            ? FieldOperationResult.Fail("Já existe atendimento para o técnico neste horário. Confirme a realocação.")
            : FieldOperationResult.Ok();
    }
}

public sealed class FieldRouteService
{
    public const string ProviderUnavailableMessage = "Estimativas automáticas de distância e tempo não estão configuradas. Organize a rota manualmente.";
    public FieldOperationResult Validate(Guid accountId, Guid routeAccountId, Guid? technicianId, Guid? teamId) =>
        accountId != routeAccountId ? FieldOperationResult.Fail("Rota não pertence à conta atual.") :
        technicianId is null && teamId is null ? FieldOperationResult.Fail("Selecione um técnico ou uma equipe para a rota.") : FieldOperationResult.Ok();
}

public sealed class FieldVisitSessionService
{
    public FieldOperationResult CanCheckIn(FieldWorkOrder order, Guid accountId, Guid technicianId, IEnumerable<FieldVisitSession> sessions)
    {
        if (order.AccountId != accountId || order.TechnicianId != technicianId) return FieldOperationResult.Fail("A ordem não está atribuída a este técnico.");
        if (sessions.Any(x => x.AccountId == accountId && x.WorkOrderId == order.Id && x.CheckedOutAt is null))
            return FieldOperationResult.Fail("Já existe check-in aberto para esta visita.");
        return FieldOperationResult.Ok();
    }

    public FieldOperationResult CanCheckOut(FieldWorkOrder order, FieldVisitSession? session, FieldVisitOutcome? outcome)
    {
        if (session is null || session.CheckedOutAt is not null) return FieldOperationResult.Fail("Faça o check-in antes de encerrar a visita.");
        if (outcome is null) return FieldOperationResult.Fail("Informe o resultado do atendimento.");
        if (order.RequiresChecklist && !order.ChecklistComplete) return FieldOperationResult.Fail("Conclua o checklist obrigatório.");
        if (order.RequiresEvidence && !order.HasEvidence) return FieldOperationResult.Fail("Anexe a evidência obrigatória.");
        if (order.RequiresSignature && !order.HasSignature) return FieldOperationResult.Fail("Colete o aceite operacional do cliente.");
        return FieldOperationResult.Ok();
    }
}

public sealed class FieldVisitEvidenceService
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
    public FieldOperationResult Validate(string fileName, long length, long maximumBytes = 10 * 1024 * 1024) =>
        !Allowed.Contains(Path.GetExtension(fileName)) ? FieldOperationResult.Fail("Tipo de arquivo não permitido.") :
        length <= 0 || length > maximumBytes ? FieldOperationResult.Fail("O arquivo excede o tamanho permitido.") : FieldOperationResult.Ok();
}

public sealed class FieldVisitSignatureService
{
    public const string LegalNotice = "Esta assinatura registra aceite operacional da execução. Certificação digital avançada depende de provedor específico configurado.";
    public FieldOperationResult Validate(string signerName, bool consent, ReadOnlyMemory<byte> drawing) =>
        !consent ? FieldOperationResult.Fail("O consentimento do assinante é obrigatório.") :
        string.IsNullOrWhiteSpace(signerName) || drawing.IsEmpty ? FieldOperationResult.Fail("Informe o assinante e capture a assinatura.") : FieldOperationResult.Ok();
}

public sealed class FieldMaterialUsageService
{
    public FieldOperationResult Validate(decimal quantity, decimal available, bool allowNegative) => quantity <= 0
        ? FieldOperationResult.Fail("A quantidade deve ser maior que zero.")
        : !allowNegative && quantity > available ? FieldOperationResult.Fail("Estoque insuficiente.") : FieldOperationResult.Ok();
}

public sealed class FieldTimeEntryService
{
    public FieldOperationResult CanStart(Guid accountId, Guid userId, IEnumerable<(Guid AccountId, Guid UserId, DateTime? EndedAt)> entries) =>
        entries.Any(x => x.AccountId == accountId && x.UserId == userId && x.EndedAt is null)
            ? FieldOperationResult.Fail("Já existe um apontamento ativo.") : FieldOperationResult.Ok();
}

public sealed class FieldOfflineSyncService
{
    public OfflineSyncState Resolve(OfflineQueueItem incoming, IEnumerable<OfflineQueueItem> persisted)
    {
        var previous = persisted.FirstOrDefault(x => x.AccountId == incoming.AccountId && x.IdempotencyKey == incoming.IdempotencyKey);
        if (previous is null) return OfflineSyncState.Applied;
        return previous.PayloadHash == incoming.PayloadHash ? previous.State : OfflineSyncState.Conflict;
    }
}

public sealed class FieldPortalIsolationService
{
    public bool CanRead(Guid currentAccountId, Guid resourceAccountId, Guid currentClientId, Guid resourceClientId) =>
        currentAccountId == resourceAccountId && currentClientId == resourceClientId;
}

public sealed class FieldQualityReviewService
{
    public FieldOperationResult Review(Guid currentAccountId, Guid orderAccountId, bool approved, string? rejectionReason) =>
        currentAccountId != orderAccountId ? FieldOperationResult.Fail("A revisão não pertence à conta atual.") :
        !approved && string.IsNullOrWhiteSpace(rejectionReason) ? FieldOperationResult.Fail("Informe o motivo para reabrir a ordem.") : FieldOperationResult.Ok();
}

public sealed class FieldVisitExpenseService
{
    public FieldOperationResult Validate(decimal amount, string category) => amount <= 0
        ? FieldOperationResult.Fail("Informe um valor maior que zero.")
        : string.IsNullOrWhiteSpace(category) ? FieldOperationResult.Fail("Selecione a categoria da despesa.") : FieldOperationResult.Ok();
}

public sealed class FieldReportService
{
    public IReadOnlyDictionary<string, int> CountByOutcome(Guid accountId, IEnumerable<(Guid AccountId, string Outcome)> rows) =>
        rows.Where(x => x.AccountId == accountId).GroupBy(x => x.Outcome)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
}
