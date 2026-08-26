using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

/// <summary>Tenant-scoped support operations. Drafts and macros are never dispatched by this service.</summary>
public sealed class SupportDeskService(OrcaFacilDbContext db)
{
    public async Task<IReadOnlyList<SupportTicketMessage>> PublicMessagesAsync(Guid accountId, Guid ticketId, CancellationToken ct = default)
    {
        var ownsTicket = await db.SupportTickets.AnyAsync(x => x.Id == ticketId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (!ownsTicket) return [];
        return await db.SupportTicketMessages.AsNoTracking()
            .Where(x => x.TicketId == ticketId && !x.IsDeleted && !x.IsInternal && x.Type != SupportMessageType.InternalNote)
            .OrderBy(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task AssignAsync(Guid accountId, Guid ticketId, Guid agentUserId, Guid actorUserId, CancellationToken ct = default)
    {
        var ticket = await TenantTicket(accountId, ticketId, ct);
        var agent = await db.SupportQueueMembers.SingleOrDefaultAsync(x => x.QueueId == ticket.QueueId && x.UserId == agentUserId && x.IsActive, ct)
            ?? throw new InvalidOperationException("O agente não está ativo na fila do chamado.");
        if (agent.MaxOpenTickets is int limit && await db.SupportTickets.CountAsync(x => x.AssignedToUserId == agentUserId && x.Status != SupportTicketStatus.Closed && x.Status != SupportTicketStatus.Resolved && !x.IsDeleted, ct) >= limit)
            throw new InvalidOperationException("O limite de chamados abertos do agente foi atingido.");
        ticket.AssignedToUserId = agentUserId; ticket.Touch();
        db.SupportTicketEvents.Add(new() { AccountId=accountId, TicketId=ticketId, ActorUserId=actorUserId, Type="Assigned", Details=$"AssignedTo:{agentUserId}" });
        await db.SaveChangesAsync(ct);
    }

    public async Task EscalateAsync(Guid accountId, Guid ticketId, Guid toQueueId, Guid actorUserId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("O motivo do escalonamento é obrigatório.", nameof(reason));
        var ticket = await TenantTicket(accountId, ticketId, ct);
        var destination = await db.SupportQueues.SingleOrDefaultAsync(x => x.Id == toQueueId && x.IsActive && (x.IsGlobal || x.AccountId == accountId), ct)
            ?? throw new InvalidOperationException("A fila de destino não está ativa ou acessível.");
        var from = ticket.QueueId ?? throw new InvalidOperationException("O chamado ainda não possui fila.");
        db.SupportTicketEscalations.Add(new() { AccountId=accountId, TicketId=ticketId, FromQueueId=from, ToQueueId=destination.Id, EscalatedByUserId=actorUserId, Reason=reason.Trim() });
        ticket.QueueId=destination.Id; ticket.AssignedToUserId=null; ticket.Status=SupportTicketStatus.Escalated; ticket.Touch();
        db.SupportTicketEvents.Add(new() { AccountId=accountId, TicketId=ticketId, ActorUserId=actorUserId, Type="Escalated", Details=reason.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task ApplySlaAsync(Guid accountId, Guid ticketId, Guid policyId, DateTime nowUtc, CancellationToken ct = default)
    {
        var ticket = await TenantTicket(accountId, ticketId, ct);
        var policy = await db.SupportSlaPolicies.SingleOrDefaultAsync(x => x.Id == policyId && x.IsActive && (x.AccountId == null || x.AccountId == accountId), ct)
            ?? throw new InvalidOperationException("Política de SLA indisponível para esta conta.");
        ticket.SlaPolicyId=policy.Id;
        ticket.FirstResponseDueAt=AddServiceMinutes(nowUtc, policy.FirstResponseMinutes, policy);
        ticket.ResolutionDueAt=AddServiceMinutes(nowUtc, policy.ResolutionMinutes, policy);
        ticket.Touch();
        db.SupportTicketSlaEvents.Add(new() { AccountId=accountId, TicketId=ticketId, Type="Started", OccurredAt=nowUtc });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RecordBreachOnceAsync(Guid accountId, Guid ticketId, string type, DateTime nowUtc, CancellationToken ct = default)
    {
        if (type is not ("FirstResponseBreached" or "ResolutionBreached")) throw new ArgumentOutOfRangeException(nameof(type));
        _ = await TenantTicket(accountId, ticketId, ct);
        if (await db.SupportTicketSlaEvents.AnyAsync(x => x.TicketId == ticketId && x.Type == type, ct)) return false;
        db.SupportTicketSlaEvents.Add(new() { AccountId=accountId, TicketId=ticketId, Type=type, OccurredAt=nowUtc });
        await db.SaveChangesAsync(ct); return true;
    }

    public async Task<string> CreateCsatSurveyAsync(Guid accountId, Guid ticketId, Guid requesterUserId, CancellationToken ct = default)
    {
        var ticket = await TenantTicket(accountId, ticketId, ct);
        if (ticket.Status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed)) throw new InvalidOperationException("CSAT só pode ser criado para chamado resolvido ou fechado.");
        if (await db.SupportCsatSurveys.AnyAsync(x=>x.TicketId==ticketId,ct)) throw new InvalidOperationException("Este chamado já possui pesquisa CSAT.");
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        db.SupportCsatSurveys.Add(new(){AccountId=accountId,TicketId=ticketId,RequesterUserId=requesterUserId,TokenHash=Hash(token),ExpiresAt=DateTime.UtcNow.AddDays(30)});
        await db.SaveChangesAsync(ct); return token;
    }

    public async Task RespondCsatAsync(string token, int rating, string? comment, bool resolved, bool timely, CancellationToken ct = default)
    {
        if (rating is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rating));
        var hash=Hash(token); var survey=await db.SupportCsatSurveys.SingleOrDefaultAsync(x=>x.TokenHash==hash,ct) ?? throw new KeyNotFoundException("Pesquisa não encontrada.");
        if (survey.ExpiresAt <= DateTime.UtcNow || survey.RespondedAt.HasValue || await db.SupportCsatResponses.AnyAsync(x=>x.SurveyId==survey.Id,ct)) throw new InvalidOperationException("Pesquisa expirada ou já respondida.");
        db.SupportCsatResponses.Add(new(){AccountId=survey.AccountId,SurveyId=survey.Id,Rating=rating,Comment=comment?.Trim(),WasResolved=resolved,TimelinessAdequate=timely});
        survey.RespondedAt=DateTime.UtcNow; survey.Touch(); await db.SaveChangesAsync(ct);
    }

    private async Task<SupportTicket> TenantTicket(Guid accountId, Guid ticketId, CancellationToken ct) =>
        await db.SupportTickets.SingleOrDefaultAsync(x=>x.Id==ticketId && x.AccountId==accountId && !x.IsDeleted,ct) ?? throw new KeyNotFoundException("Chamado não encontrado nesta conta.");
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static DateTime AddServiceMinutes(DateTime start, int minutes, SupportSlaPolicy p)
    {
        if (!p.BusinessHoursOnly) return start.AddMinutes(minutes);
        var cursor=start; var remaining=minutes; var open=p.StartTime ?? new(8,0); var close=p.EndTime ?? new(18,0);
        while(remaining>0) {
            if(cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || TimeOnly.FromDateTime(cursor)>=close) { cursor=cursor.Date.AddDays(1).Add(open.ToTimeSpan()); continue; }
            if(TimeOnly.FromDateTime(cursor)<open) cursor=cursor.Date.Add(open.ToTimeSpan());
            var available=(int)(cursor.Date.Add(close.ToTimeSpan())-cursor).TotalMinutes; var used=Math.Min(remaining,available); cursor=cursor.AddMinutes(used); remaining-=used;
        }
        return cursor;
    }
}
