namespace OrcaFacil.Application.Documents;

public record DeleteDocumentCommand(Guid UserId, Guid DocumentId, Guid? AccountId = null);
