using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Abstractions;

public interface IPdfService
{
    Task<byte[]> GenerateDocumentPdfAsync(Document document, IssuerProfile? issuer, PlanType plan, CancellationToken ct = default);
}
