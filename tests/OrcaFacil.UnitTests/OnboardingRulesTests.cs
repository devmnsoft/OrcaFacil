using OrcaFacil.Application.Onboarding;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class OnboardingRulesTests
{
    [Theory]
    [InlineData(OnboardingStep.Welcome, false)]
    [InlineData(OnboardingStep.BusinessProfile, true)]
    [InlineData(OnboardingStep.DocumentIdentity, true)]
    [InlineData(OnboardingStep.FirstClient, false)]
    [InlineData(OnboardingStep.FirstService, false)]
    public void Required_steps_are_only_the_mandatory_budget_data(OnboardingStep step, bool required)
    {
        Assert.Equal(required, OnboardingStepRules.IsRequired(step));
        Assert.Equal(!required, OnboardingStepRules.CanSkip(step));
    }

    [Fact]
    public void Completed_step_cannot_be_skipped_and_skipping_does_not_produce_an_invalid_step()
    {
        Assert.False(OnboardingStepRules.CanSkip(OnboardingStep.Completed));

        Assert.Equal(OnboardingStep.Welcome, OnboardingStepRules.NextAfterSkip(OnboardingStep.Welcome));
        Assert.Equal(OnboardingStep.FirstService, OnboardingStepRules.NextAfterSkip(OnboardingStep.FirstClient));
        Assert.Equal(OnboardingStep.Completed, OnboardingStepRules.NextAfterSkip(OnboardingStep.FirstBudget));
    }

    [Fact]
    public void Onboarding_state_advances_skips_and_completes()
    {
        var state = new AccountOnboardingState { AccountId = Guid.NewGuid(), UserId = Guid.NewGuid() };

        Assert.Equal(OnboardingStep.Welcome, state.CurrentStep);
        Assert.Null(state.CompletedAt);
        Assert.Null(state.SkippedAt);

        state.Advance(OnboardingStep.FirstClient);
        Assert.Equal(OnboardingStep.FirstClient, state.CurrentStep);
        Assert.NotNull(state.UpdatedAt);

        state.Skip();
        Assert.NotNull(state.SkippedAt);

        state.Complete();
        Assert.Equal(OnboardingStep.Completed, state.CurrentStep);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public void Persistence_keeps_the_idempotent_account_user_upsert_boundary()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var source = File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "OnboardingApplicationService.cs"));

        Assert.Contains("ON CONFLICT (account_id, user_id) DO UPDATE", source);
        Assert.Contains("OnboardingStepRules.IsRequired(step)", source);
        Assert.Contains("OnboardingStepRules.CanSkip(step)", source);
    }
}
