using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public class PlanLimitService
{
    public bool CanCreateDocument(PlanType plan, int monthlyCount, int freeLimit = 20) => plan != PlanType.Free || monthlyCount < freeLimit;
    public bool CanGeneratePdf(PlanType plan, int monthlyCount, int freeLimit = 20) => plan != PlanType.Free || monthlyCount < freeLimit;
    public bool PdfHasWatermark(PlanType plan) => plan == PlanType.Free;
}
