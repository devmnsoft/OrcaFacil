using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Privacy;

public sealed class AnonymizationService(IRepository<Client> clients, IUnitOfWork unitOfWork, IAuditService audit)
{
    public const string ConfirmationPhrase = "ANONIMIZAR DEFINITIVAMENTE";

    public async Task AnonymizeClientAsync(Guid accountId, Guid actorUserId, Guid clientId, string confirmation,
        CancellationToken ct = default)
    {
        if (!string.Equals(confirmation, ConfirmationPhrase, StringComparison.Ordinal))
            throw new InvalidOperationException($"Digite {ConfirmationPhrase} para confirmar.");
        var client = await clients.GetAsync(clientId, ct);
        if (client is null || client.AccountId != accountId || client.IsDeleted)
            throw new UnauthorizedAccessException("Cliente não pertence à conta informada.");
        var before = new { client.Name, HasDocument = !string.IsNullOrWhiteSpace(client.DocumentNumber),
            HasEmail = !string.IsNullOrWhiteSpace(client.Email), HasPhone = !string.IsNullOrWhiteSpace(client.Phone) };
        client.Name = "Cliente anonimizado";
        client.TradeName = null; client.LegalName = null; client.DocumentNumber = null;
        client.Email = $"anonimizado+{client.Id:N}@orcafacil.local"; client.Phone = null;
        client.Address = null; client.City = null; client.Notes = null; client.InternalNotes = null;
        client.Touch();
        await audit.RegisterAsync(actorUserId, "Privacy.ClientAnonymized", nameof(Client), client.Id.ToString(), before,
            new { client.Name, Anonymized = true }, new { Irreversible = true }, ct, accountId);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
