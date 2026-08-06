using OrcaFacil.Application.Common;
using OrcaFacil.Domain.Enums;
namespace OrcaFacil.Application.Onboarding;
public sealed record OnboardingStateView(OnboardingStep CurrentStep, int CompletedSteps, int TotalSteps, bool IsCompleted, DateTime? SkippedAt, NextActionDescriptor NextAction, Guid? FirstClientId, Guid? FirstServiceId);
public sealed record BusinessProfileInput(PersonType PersonType, string Name, string Document, string Phone, string? WhatsApp, string Email, string City, string State);
public sealed record DocumentIdentityInput(string DisplayName, string? Phone, string? Email, string? City, string? Address, string? PixKey, string? DefaultNote);
public sealed record FirstClientInput(string Name, PersonType PersonType, string? Document, string? Phone, string? WhatsApp, string? Email, string? City);
public sealed record FirstServiceInput(string Name, string? Category, string Unit, decimal Price, decimal? Cost, int? DurationMinutes, string? Description);
public interface IOnboardingApplicationService
{
 Task<OperationResult<OnboardingStateView>> GetAsync(CancellationToken ct = default);
 Task<OperationResult> BeginAsync(CancellationToken ct = default);
 Task<OperationResult> SkipAsync(OnboardingStep step, CancellationToken ct = default);
 Task<OperationResult> SaveBusinessAsync(BusinessProfileInput input, CancellationToken ct = default);
 Task<OperationResult> SaveDocumentIdentityAsync(DocumentIdentityInput input, CancellationToken ct = default);
 Task<OperationResult<Guid>> CreateClientAsync(FirstClientInput input, CancellationToken ct = default);
 Task<OperationResult<Guid>> CreateServiceAsync(FirstServiceInput input, CancellationToken ct = default);
 Task<OperationResult<Guid?>> StartBudgetAsync(CancellationToken ct = default);
 Task<OperationResult> CompleteAsync(CancellationToken ct = default);
}
