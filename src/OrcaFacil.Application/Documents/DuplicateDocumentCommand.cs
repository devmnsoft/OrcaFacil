namespace OrcaFacil.Application.Documents;

public record DuplicateDocumentCommand(Guid UserId, Guid DocumentId, Guid? AccountId = null);
