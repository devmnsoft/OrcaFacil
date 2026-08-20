using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Privacy;

public sealed class DataSubjectRequestService(IRepository<DataSubjectRequest> requests, IUnitOfWork unitOfWork, IAuditService audit)
{
    public async Task<DataSubjectRequest> OpenAsync(Guid accountId, Guid userId, DataSubjectRequestType type,
        string description, Guid? clientId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Descreva a solicitação.");
        var request = new DataSubjectRequest { AccountId = accountId, RequesterUserId = userId, Type = type,
            Description = description.Trim(), RequestedAt = DateTime.UtcNow, DueAt = DateTime.UtcNow.AddDays(15),
            Status = DataSubjectRequestStatus.Received, ClientId = clientId, CorrelationId = Guid.NewGuid() };
        await requests.AddAsync(request, ct);
        await audit.RegisterAsync(userId, "Privacy.RequestOpened", nameof(DataSubjectRequest), request.Id.ToString(), null,
            new { request.Type, request.Status }, null, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
        return request;
    }

    public async Task ResolveAsync(Guid accountId, Guid reviewerId, Guid requestId, bool approved, string notes,
        CancellationToken ct = default)
    {
        var request = await requests.GetAsync(requestId, ct);
        if (request is null || request.AccountId != accountId) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(notes)) throw new ArgumentException("A decisão precisa de justificativa.");
        var previousStatus = request.Status;
        request.Status = approved ? DataSubjectRequestStatus.Completed : DataSubjectRequestStatus.Rejected;
        request.ResolutionNotes = notes.Trim(); request.ReviewedAt = DateTime.UtcNow;
        if (approved) request.CompletedAt = DateTime.UtcNow; else { request.RejectedAt = DateTime.UtcNow; request.RejectionReason = notes.Trim(); }
        request.Touch();
        await audit.RegisterAsync(reviewerId, "Privacy.RequestResolved", nameof(DataSubjectRequest), request.Id.ToString(),
            new { Status = previousStatus }, new { request.Status }, new { DecisionRecorded = true }, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
