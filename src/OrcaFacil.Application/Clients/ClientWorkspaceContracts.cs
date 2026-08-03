using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Clients;

public enum ClientResultCode { Success, AccountRequired, AccessDenied, ClientNotFound, ContactNotFound, TagNotFound, DuplicateDocument, PossibleDuplicate, LimitReached, InvalidInput, ConcurrencyConflict, Unexpected }

public sealed record ClientWorkspaceQuery(string? Search = null, PersonType? PersonType = null, string? City = null,
    Guid? TagId = null, bool? Favorite = null, bool? Active = null, string Sort = "name", int Page = 1, int PageSize = 20);
public sealed record ClientWorkspaceItem(Guid Id, PersonType PersonType, BrazilianDocumentType? DocumentType, string? DocumentNumber,
    string Name, string? TradeName, string? City, bool IsFavorite, bool IsActive, string? PrimaryContact,
    IReadOnlyList<ClientTagSummary> Tags, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record ClientTagSummary(Guid Id, string Name, string ColorToken);
public sealed record ClientWorkspaceResult(ClientResultCode Code, IReadOnlyList<ClientWorkspaceItem> Items, int Total,
    int Active, int Favorites, int NewThisMonth, int Incomplete, int Page, int PageSize)
{ public bool Succeeded => Code == ClientResultCode.Success; public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)); }
public sealed record ClientSaveResult(ClientResultCode Code, Guid? ClientId = null, string? Message = null, IReadOnlyList<DuplicateClientCandidate>? Candidates = null);
public sealed record ClientContactResult(ClientResultCode Code, Guid? ContactId = null, string? Message = null);
public sealed record ClientTagResult(ClientResultCode Code, Guid? TagId = null, string? Message = null);
public sealed record ClientNoteResult(ClientResultCode Code, Guid? NoteId = null, string? Message = null);
public sealed record DuplicateClientCandidate(Guid Id, string Name, string MatchReason);
public sealed record ClientContactInput(string Name, ClientContactType ContactType, string Value, string? Label, bool IsPrimary, bool ReceivesQuotes, bool ReceivesReceipts);
public sealed record ClientWorkspaceDetails(Client Client, IReadOnlyList<ClientContact> Contacts,
    IReadOnlyList<ClientTagSummary> Tags, IReadOnlyList<ClientNote> Notes);

public interface IClientWorkspaceService
{
    Task<ClientWorkspaceResult> ListAsync(ClientWorkspaceQuery query, CancellationToken ct = default);
    Task<ClientWorkspaceDetails?> GetAsync(Guid clientId, CancellationToken ct = default);
    Task<ClientWorkspaceDetails?> GetDetailsAsync(Guid clientId, CancellationToken ct = default);
    Task<ClientSaveResult> CreateAsync(Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default);
    Task<ClientSaveResult> UpdateAsync(Guid clientId, Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default);
    Task<ClientSaveResult> SaveAsync(Client input, bool allowPossibleDuplicate = false, CancellationToken ct = default);
    Task<ClientSaveResult> ToggleFavoriteAsync(Guid clientId, CancellationToken ct = default);
    Task<ClientSaveResult> SetActiveAsync(Guid clientId, bool active, CancellationToken ct = default);
    Task<ClientSaveResult> DeleteAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<DuplicateClientCandidate>> FindDuplicatesAsync(Client input, CancellationToken ct = default);
    Task<ClientContactResult> AddContactAsync(Guid clientId, ClientContactInput input, CancellationToken ct = default);
    Task<ClientContactResult> RemoveContactAsync(Guid clientId, Guid contactId, CancellationToken ct = default);
    Task<ClientTagResult> CreateAndAssignTagAsync(Guid clientId, string name, string colorToken, CancellationToken ct = default);
    Task<ClientTagResult> RemoveTagAsync(Guid clientId, Guid tagId, CancellationToken ct = default);
    Task<ClientNoteResult> AddNoteAsync(Guid clientId, string content, bool pinned, CancellationToken ct = default);
    Task<ClientNoteResult> ToggleNotePinAsync(Guid clientId, Guid noteId, CancellationToken ct = default);
    Task<ClientNoteResult> DeleteNoteAsync(Guid clientId, Guid noteId, CancellationToken ct = default);
}
