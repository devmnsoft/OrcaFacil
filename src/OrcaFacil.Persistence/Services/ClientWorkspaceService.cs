using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Clients;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Persistence.Services;

public sealed class ClientWorkspaceService(OrcaFacilDbContext db, ICurrentAccountService current) : IClientWorkspaceService
{
    private static readonly HashSet<string> Colors = ["neutral", "blue", "green", "amber", "red", "purple", "cyan"];
    private Guid? AccountId => current.AccountId;

    public async Task<ClientWorkspaceDetails?> GetDetailsAsync(Guid clientId, CancellationToken ct = default)
    {
        if (AccountId is not Guid accountId) return null;
        var client = await db.Clients.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clientId && x.AccountId == accountId && !x.IsDeleted, ct);
        if (client is null) return null;
        var contacts = await db.ClientContacts.AsNoTracking().Where(x => x.AccountId == accountId && x.ClientId == clientId && !x.IsDeleted).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder)
            .Select(x => new ClientContactSummary(x.Id, x.Name, x.ContactType, x.Value, x.Label, x.IsPrimary, x.ReceivesQuotes, x.ReceivesReceipts, x.IsActive, x.SortOrder)).Take(50).ToListAsync(ct);
        var tags = await (from assignment in db.ClientTagAssignments.AsNoTracking()
            join tag in db.ClientTags.AsNoTracking() on assignment.ClientTagId equals tag.Id
            where assignment.AccountId == accountId && assignment.ClientId == clientId && !tag.IsDeleted
            orderby tag.Name
            select new ClientTagSummary(tag.Id, tag.Name, tag.ColorToken)).Take(30).ToListAsync(ct);
        var notes = await db.ClientNotes.AsNoTracking().Where(x => x.AccountId == accountId && x.ClientId == clientId && !x.IsDeleted).OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt)
            .Select(x => new ClientNoteSummary(x.Id, x.Content, x.IsPinned, x.CreatedByUserId, x.CreatedAt, x.UpdatedAt)).Take(50).ToListAsync(ct);
        var profile = new ClientProfileSummary(client.Id, client.PersonType, client.DocumentType,
            BrazilianDocument.Mask(client.DocumentType, client.DocumentNumber), client.Name, client.LegalName,
            client.TradeName, client.City, client.Address, client.IsActive, client.IsFavorite,
            client.PreferredContactChannel, client.LastInteractionAt, client.NextFollowUpAt,
            client.CreatedAt, client.UpdatedAt, client.Version);
        return new ClientWorkspaceDetails(profile, contacts, tags, notes,
            new(0, 0, 0, 0, 0, 0, 0, 0, null, client.NextFollowUpAt),
            new(0, 0, 0, 0, 0, 0, 0, null), []);
    }

    public async Task<ClientWorkspaceResult> ListAsync(ClientWorkspaceQuery request, CancellationToken ct = default)
    {
        if (AccountId is not Guid accountId) return new(ClientResultCode.AccountRequired, [], 0, 0, 0, 0, 0, 1, request.PageSize);
        var query = db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Search)) { var term = $"%{request.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Name, term) || (x.TradeName != null && EF.Functions.ILike(x.TradeName, term)) || (x.DocumentNumber != null && x.DocumentNumber.Contains(request.Search)) || (x.Email != null && EF.Functions.ILike(x.Email, term)) || (x.Phone != null && x.Phone.Contains(request.Search)) || db.ClientContacts.Any(c => c.AccountId == accountId && c.ClientId == x.Id && !c.IsDeleted && (EF.Functions.ILike(c.Name, term) || EF.Functions.ILike(c.Value, term))) || db.ClientTagAssignments.Any(a => a.AccountId == accountId && a.ClientId == x.Id && db.ClientTags.Any(t => t.Id == a.ClientTagId && EF.Functions.ILike(t.Name, term)))); }
        if (request.PersonType.HasValue) query = query.Where(x => x.PersonType == request.PersonType);
        if (!string.IsNullOrWhiteSpace(request.City)) query = query.Where(x => x.City != null && EF.Functions.ILike(x.City, $"%{request.City.Trim()}%"));
        if (request.Favorite.HasValue) query = query.Where(x => x.IsFavorite == request.Favorite);
        if (request.Active.HasValue) query = query.Where(x => x.IsActive == request.Active);
        if (request.TagId.HasValue) query = query.Where(x => db.ClientTagAssignments.Any(a => a.AccountId == accountId && a.ClientId == x.Id && a.ClientTagId == request.TagId));
        var total = await query.CountAsync(ct); var pageSize = Math.Clamp(request.PageSize, 10, 100); var page = Math.Max(request.Page, 1);
        query = request.Sort switch { "recent" => query.OrderByDescending(x => x.CreatedAt), "activity" => query.OrderByDescending(x => x.LastInteractionAt), "name_desc" => query.OrderByDescending(x => x.Name), _ => query.OrderBy(x => x.Name) };
        var clients = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct); var ids = clients.Select(x => x.Id).ToArray();
        var contacts = await db.ClientContacts.AsNoTracking().Where(x => x.AccountId == accountId && ids.Contains(x.ClientId) && x.IsPrimary && !x.IsDeleted).ToDictionaryAsync(x => x.ClientId, x => x.Value, ct);
        var assignments = await (from a in db.ClientTagAssignments where a.AccountId == accountId && ids.Contains(a.ClientId) join t in db.ClientTags on a.ClientTagId equals t.Id where !t.IsDeleted select new { a.ClientId, Tag = new ClientTagSummary(t.Id, t.Name, t.ColorToken) }).ToListAsync(ct);
        var items = clients.Select(x => new ClientWorkspaceItem(x.Id, x.PersonType, x.DocumentType, x.DocumentNumber, x.Name, x.TradeName, x.City, x.IsFavorite, x.IsActive, contacts.GetValueOrDefault(x.Id), assignments.Where(a => a.ClientId == x.Id).Select(a => a.Tag).ToList(), x.CreatedAt, x.UpdatedAt)).ToList();
        var all = db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
        return new(ClientResultCode.Success, items, total, await all.CountAsync(x => x.IsActive, ct), await all.CountAsync(x => x.IsFavorite, ct), await all.CountAsync(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-30), ct), await all.CountAsync(x => x.Email == null && x.Phone == null, ct), page, pageSize);
    }

    public Task<ClientSaveResult> SaveAsync(Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default) =>
        CreateAsync(input, allowPossibleDuplicate, ct);

    public async Task<ClientSaveResult> CreateAsync(Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default)
    {
        if (AccountId is not Guid accountId) return new(ClientResultCode.AccountRequired, Message: "Selecione uma conta.");
        if (string.IsNullOrWhiteSpace(input.Name)) return new(ClientResultCode.InvalidInput, Message: "Informe o nome do cliente.");
        try { input.NormalizeAndValidate(); } catch (InvalidOperationException ex) { return new(ClientResultCode.InvalidInput, Message: ex.Message); }
        var duplicates = await FindDuplicatesAsync(input, ct); var exact = duplicates.FirstOrDefault(x => x.MatchReason == "Documento idêntico");
        if (exact is not null) return new(ClientResultCode.DuplicateDocument, exact.Id, "CPF/CNPJ já cadastrado.", duplicates);
        if (duplicates.Count > 0 && !allowPossibleDuplicate) return new(ClientResultCode.PossibleDuplicate, Candidates: duplicates, Message: "Encontramos clientes parecidos.");
        input.AccountId = accountId; input.UserId = current.UserId; db.Clients.Add(input); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, input.Id);
    }

    public Task<ClientSaveResult> CreateAsync(CreateClientRequest r, CancellationToken ct = default) => CreateAsync(new Client { PersonType=r.PersonType,DocumentType=r.DocumentType,DocumentNumber=r.DocumentNumber,Name=r.Name,LegalName=r.LegalName,TradeName=r.TradeName,Email=r.Email,Phone=r.Phone,City=r.City,Address=r.Address,InternalNotes=r.InternalNotes,PreferredContactChannel=r.PreferredContactChannel,NextFollowUpAt=r.NextFollowUpAt,IsFavorite=r.IsFavorite,IsActive=r.IsActive },r.AllowPossibleDuplicate,ct);
    public Task<ClientSaveResult> UpdateAsync(UpdateClientRequest r, CancellationToken ct = default) => UpdateAsync(r.ClientId,new Client { PersonType=r.PersonType,DocumentType=r.DocumentType,DocumentNumber=r.DocumentNumber,Name=r.Name,LegalName=r.LegalName,TradeName=r.TradeName,Email=r.Email,Phone=r.Phone,City=r.City,Address=r.Address,InternalNotes=r.InternalNotes,PreferredContactChannel=r.PreferredContactChannel,NextFollowUpAt=r.NextFollowUpAt,IsFavorite=r.IsFavorite,IsActive=r.IsActive,Version=r.ExpectedVersion },r.AllowPossibleDuplicate,ct);

    public async Task<ClientSaveResult> UpdateAsync(Guid clientId, Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default)
    {
        if (AccountId is not Guid accountId)
            return new(ClientResultCode.AccountRequired, Message: "Selecione uma conta.");

        var client = await db.Clients.SingleOrDefaultAsync(
            x => x.Id == clientId && x.AccountId == accountId && !x.IsDeleted,
            ct);
        if (client is null)
            return new(ClientResultCode.ClientNotFound);

        try
        {
            input.NormalizeAndValidate();
        }
        catch (InvalidOperationException ex)
        {
            return new(ClientResultCode.InvalidInput, Message: ex.Message);
        }

        var duplicates = await FindDuplicatesAsync(input, clientId, ct);
        var exact = duplicates.FirstOrDefault(x => x.MatchReason == "Documento idêntico");
        if (exact is not null)
            return new(ClientResultCode.DuplicateDocument, exact.Id, "CPF/CNPJ já cadastrado.", duplicates);
        if (duplicates.Count > 0 && !allowPossibleDuplicate)
            return new(ClientResultCode.PossibleDuplicate, clientId, "Encontramos clientes parecidos.", duplicates);

        client.PersonType = input.PersonType;
        client.DocumentType = input.DocumentType;
        client.DocumentNumber = input.DocumentNumber;
        client.Name = input.Name.Trim();
        client.LegalName = input.LegalName?.Trim();
        client.TradeName = input.TradeName?.Trim();
        client.Email = input.Email?.Trim();
        client.Phone = input.Phone?.Trim();
        client.City = input.City?.Trim();
        client.Address = input.Address?.Trim();
        client.Notes = input.Notes?.Trim();
        client.InternalNotes = input.InternalNotes?.Trim();
        client.PreferredContactChannel = input.PreferredContactChannel;
        client.NextFollowUpAt = input.NextFollowUpAt;
        client.IsFavorite = input.IsFavorite;
        client.IsActive = input.IsActive;
        client.Touch();

        db.Entry(client).Property(x => x.Version).OriginalValue = input.Version;
        try
        {
            await db.SaveChangesAsync(ct);
            return new(ClientResultCode.Success, clientId);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new(ClientResultCode.ConcurrencyConflict, clientId,
                "Este cliente foi alterado por outra pessoa. Recarregue a página antes de salvar novamente.");
        }
    }

    public async Task<IReadOnlyList<DuplicateClientCandidate>> FindDuplicatesAsync(Client input, CancellationToken ct = default)
    {
        return await FindDuplicatesAsync(input, input.Id, ct);
    }

    private async Task<IReadOnlyList<DuplicateClientCandidate>> FindDuplicatesAsync(
        Client input,
        Guid excludedClientId,
        CancellationToken ct)
    {
        if (AccountId is not Guid accountId)
            return [];

        var document = BrazilianDocument.Normalize(input.DocumentNumber);
        var email = input.Email?.Trim();
        var phone = input.Phone?.Trim();
        var name = input.Name.Trim().ToUpper();
        return await db.Clients.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted && x.Id != excludedClientId &&
                ((!string.IsNullOrEmpty(document) && x.DocumentNumber == document) ||
                 x.Name.ToUpper() == name ||
                 (!string.IsNullOrWhiteSpace(email) && x.Email == email) ||
                 (!string.IsNullOrWhiteSpace(phone) && x.Phone == phone)))
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new DuplicateClientCandidate(
                x.Id,
                x.Name,
                !string.IsNullOrEmpty(document) && x.DocumentNumber == document
                    ? "Documento idêntico"
                    : x.Email == email
                        ? "E-mail igual"
                        : x.Phone == phone
                            ? "Telefone igual"
                            : "Nome semelhante"))
            .Take(5)
            .ToListAsync(ct);
    }
    public async Task<ClientSaveResult> ToggleFavoriteAsync(Guid id, CancellationToken ct = default) { var c = await Client(id, ct); if (c is null) return new(ClientResultCode.ClientNotFound); c.IsFavorite = !c.IsFavorite; c.Touch(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    public async Task<ClientSaveResult> SetActiveAsync(Guid id, bool active, CancellationToken ct = default) { var c = await Client(id, ct); if (c is null) return new(ClientResultCode.ClientNotFound); c.IsActive = active; c.Touch(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    public async Task<ClientSaveResult> DeleteAsync(Guid id, CancellationToken ct = default) { var c = await Client(id, ct); if (c is null) return new(ClientResultCode.ClientNotFound); c.MarkAsDeleted(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    public async Task<ClientContactResult> AddContactAsync(Guid clientId, ClientContactInput i, CancellationToken ct = default) { if (AccountId is not Guid aid || await Client(clientId, ct) is null) return new(ClientResultCode.ClientNotFound); if (string.IsNullOrWhiteSpace(i.Value)) return new(ClientResultCode.InvalidInput, Message: "Informe o contato."); if (i.IsPrimary) await db.ClientContacts.Where(x => x.AccountId == aid && x.ClientId == clientId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false), ct); var c = new ClientContact { AccountId = aid, ClientId = clientId, Name = i.Name.Trim(), ContactType = i.ContactType, Value = i.Value.Trim(), Label = i.Label, IsPrimary = i.IsPrimary, ReceivesQuotes = i.ReceivesQuotes, ReceivesReceipts = i.ReceivesReceipts }; db.Add(c); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, c.Id); }
    public async Task<ClientContactResult> RemoveContactAsync(Guid clientId, Guid id, CancellationToken ct = default) { if (AccountId is not Guid aid) return new(ClientResultCode.AccountRequired); var c = await db.ClientContacts.SingleOrDefaultAsync(x => x.Id == id && x.ClientId == clientId && x.AccountId == aid && !x.IsDeleted, ct); if (c is null) return new(ClientResultCode.ContactNotFound); if (c.IsPrimary && c.IsActive) return new(ClientResultCode.InvalidInput, Message: "Defina outro contato principal antes de remover este contato."); c.MarkAsDeleted(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    public async Task<ClientTagResult> CreateAndAssignTagAsync(Guid clientId, string name, string color, CancellationToken ct = default) { if (AccountId is not Guid aid || await Client(clientId, ct) is null) return new(ClientResultCode.ClientNotFound); if (!Colors.Contains(color)) return new(ClientResultCode.InvalidInput, Message: "Cor de tag inválida."); var normalized = name.Trim().ToUpperInvariant(); var tag = await db.ClientTags.SingleOrDefaultAsync(x => x.AccountId == aid && x.NormalizedName == normalized && !x.IsDeleted, ct); if (tag is null) { tag = new ClientTag { AccountId = aid, Name = name.Trim(), NormalizedName = normalized, ColorToken = color }; db.Add(tag); } if (!await db.ClientTagAssignments.AnyAsync(x => x.AccountId == aid && x.ClientId == clientId && x.ClientTagId == tag.Id, ct)) db.Add(new ClientTagAssignment { AccountId = aid, ClientId = clientId, ClientTagId = tag.Id }); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, tag.Id); }
    public async Task<ClientTagResult> RemoveTagAsync(Guid clientId, Guid tagId, CancellationToken ct = default) { if (AccountId is not Guid aid) return new(ClientResultCode.AccountRequired); var a = await db.ClientTagAssignments.SingleOrDefaultAsync(x => x.AccountId == aid && x.ClientId == clientId && x.ClientTagId == tagId, ct); if (a is null) return new(ClientResultCode.TagNotFound); db.Remove(a); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, tagId); }
    public async Task<ClientNoteResult> AddNoteAsync(Guid clientId, string content, bool pinned, CancellationToken ct = default) { if (AccountId is not Guid aid || await Client(clientId, ct) is null) return new(ClientResultCode.ClientNotFound); if (string.IsNullOrWhiteSpace(content)) return new(ClientResultCode.InvalidInput, Message: "Escreva a observação."); var n = new ClientNote { AccountId = aid, ClientId = clientId, Content = content.Trim(), IsPinned = pinned, CreatedByUserId = current.UserId }; db.Add(n); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, n.Id); }
    public async Task<ClientNoteResult> ToggleNotePinAsync(Guid clientId, Guid id, CancellationToken ct = default) { var n = await Note(clientId, id, ct); if (n is null) return new(ClientResultCode.ClientNotFound); n.IsPinned = !n.IsPinned; n.Touch(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    public async Task<ClientNoteResult> DeleteNoteAsync(Guid clientId, Guid id, CancellationToken ct = default) { var n = await Note(clientId, id, ct); if (n is null) return new(ClientResultCode.ClientNotFound); n.MarkAsDeleted(); await db.SaveChangesAsync(ct); return new(ClientResultCode.Success, id); }
    private Task<Client?> Client(Guid id, CancellationToken ct) => AccountId is Guid aid ? db.Clients.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == aid && !x.IsDeleted, ct) : Task.FromResult<Client?>(null);
    private Task<ClientNote?> Note(Guid cid, Guid id, CancellationToken ct) => AccountId is Guid aid ? db.ClientNotes.SingleOrDefaultAsync(x => x.Id == id && x.ClientId == cid && x.AccountId == aid && !x.IsDeleted, ct) : Task.FromResult<ClientNote?>(null);
}
