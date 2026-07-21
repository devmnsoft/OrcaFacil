using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Plans;

public class PlanLimitService
{
    public bool CanCreateDocument(PlanType plan, int monthlyCount, int freeLimit = 5) => plan == PlanType.Pro || monthlyCount < freeLimit;
    public bool PdfHasWatermark(PlanType plan) => plan == PlanType.Free;
}
