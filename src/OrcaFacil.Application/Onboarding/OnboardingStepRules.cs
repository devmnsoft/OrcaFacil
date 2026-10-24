using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Onboarding;

/// <summary>
/// Pure rules for the guided onboarding steps. Kept free of persistence so they can be verified
/// without a database and reused by the application service.
/// </summary>
public static class OnboardingStepRules
{
    /// <summary>Steps that prepare mandatory data for a budget and therefore cannot be skipped.</summary>
    public static bool IsRequired(OnboardingStep step) =>
        step is OnboardingStep.BusinessProfile or OnboardingStep.DocumentIdentity;

    /// <summary>Next step after skipping <paramref name="step"/>; skipping Welcome keeps Welcome.</summary>
    public static OnboardingStep NextAfterSkip(OnboardingStep step) =>
        step == OnboardingStep.Welcome ? OnboardingStep.Welcome : (OnboardingStep)((int)step + 1);

    /// <summary>A step can be skipped only when it is optional and has a successor.</summary>
    public static bool CanSkip(OnboardingStep step) =>
        !IsRequired(step) && step != OnboardingStep.Completed;
}
