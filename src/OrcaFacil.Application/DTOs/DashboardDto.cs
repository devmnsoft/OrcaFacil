namespace OrcaFacil.Application.DTOs;

public record DashboardDto(int TotalDocuments, int TotalBudgets, int TotalReceipts, decimal BudgetTotal, decimal ReceiptTotal, int DocumentsThisMonth, int PdfsThisMonth, string Plan, IReadOnlyList<DocumentSummaryDto> LatestDocuments);
