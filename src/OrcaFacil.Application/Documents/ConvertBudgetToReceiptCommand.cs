namespace OrcaFacil.Application.Documents;

public record ConvertBudgetToReceiptCommand(Guid UserId, Guid DocumentId);
