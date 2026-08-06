using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Domain.Entities;
public sealed class AccountOnboardingState : Entity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public OnboardingStep CurrentStep { get; set; } = OnboardingStep.Welcome;
    public DateTime? BusinessProfileCompletedAt { get; set; }
    public DateTime? IssuerProfileCompletedAt { get; set; }
    public DateTime? FirstClientCompletedAt { get; set; }
    public DateTime? FirstServiceCompletedAt { get; set; }
    public DateTime? FirstBudgetStartedAt { get; set; }
    public DateTime? FirstBudgetCompletedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SkippedAt { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public void Advance(OnboardingStep step) { CurrentStep = step; LastSeenAt = DateTime.UtcNow; Touch(); }
    public void Skip() { SkippedAt = LastSeenAt = DateTime.UtcNow; Touch(); }
    public void Complete() { CurrentStep = OnboardingStep.Completed; CompletedAt = LastSeenAt = DateTime.UtcNow; Touch(); }
}
