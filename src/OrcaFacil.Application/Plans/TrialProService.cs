using Microsoft.Extensions.Options;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Shared;

namespace OrcaFacil.Application.Plans;

public sealed class TrialProService
{
    private readonly IRepository<Subscription> _subscriptions;
    private readonly IRepository<UserAccount> _users;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;
    private readonly PlanOptions _options;

    public TrialProService(IRepository<Subscription> subscriptions, IRepository<UserAccount> users, INotificationService notifications, IUnitOfWork uow, IOptions<PlanOptions> options)
    {
        _subscriptions = subscriptions;
        _users = users;
        _notifications = notifications;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<Result> ActivateManualTrialAsync(Guid userId, Guid adminUserId, CancellationToken ct = default)
    {
        var user = await _users.GetAsync(userId, ct);
        if (user is null || user.IsDeleted) return Result.Fail("Usuário não encontrado.");

        var subscription = _subscriptions.Query().SingleOrDefault(x => x.UserId == userId && !x.IsDeleted)
            ?? new Subscription { UserId = userId, Provider = "Manual", Plan = PlanType.Professional };
        if (subscription.Id != Guid.Empty && subscription.TrialUsed) return Result.Fail("Trial Pro já utilizado.");

        var now = DateTime.UtcNow;
        subscription.Plan = PlanType.Professional;
        subscription.Status = SubscriptionStatus.Trial;
        subscription.TrialStartedAt = now;
        subscription.TrialEndsAt = now.AddDays(Math.Max(1, _options.TrialProDays));
        subscription.TrialUsed = true;
        subscription.TrialStatus = TrialStatus.Active;
        subscription.StartedAt ??= now;
        subscription.ExpiresAt = subscription.TrialEndsAt;
        subscription.Touch();
        user.Plan = PlanType.Professional;
        user.Touch();

        if (!_subscriptions.Query().Any(x => x.Id == subscription.Id)) await _subscriptions.AddAsync(subscription, ct);
        await _notifications.CreateForUserAsync(userId, "Trial Pro ativado", $"Seu teste Pro foi ativado até {subscription.TrialEndsAt:dd/MM/yyyy}. Aproveite os benefícios premium por tempo limitado.", NotificationType.Success, NotificationCategory.Plan, "/Subscription", "Ver assinatura", ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> ActivateManualTrialForAccountAsync(Guid accountId, Guid adminUserId, int days, CancellationToken ct = default)
    {
        var subscription = _subscriptions.Query().SingleOrDefault(x => x.AccountId == accountId && !x.IsDeleted)
            ?? new Subscription { AccountId = accountId, Provider = "Manual", Plan = PlanType.Professional };

        var now = DateTime.UtcNow;
        subscription.Plan = PlanType.Professional;
        subscription.Status = SubscriptionStatus.Trial;
        subscription.TrialStartedAt = now;
        subscription.TrialEndsAt = now.AddDays(Math.Max(1, days > 0 ? days : _options.TrialProDays));
        subscription.TrialUsed = true;
        subscription.TrialStatus = TrialStatus.Active;
        subscription.StartedAt ??= now;
        subscription.ExpiresAt = subscription.TrialEndsAt;
        subscription.Touch();

        if (!_subscriptions.Query().Any(x => x.Id == subscription.Id))
            await _subscriptions.AddAsync(subscription, ct);

        await _uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<int> ExpireTrialsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = _subscriptions.Query().Where(x => x.TrialStatus == TrialStatus.Active && x.TrialEndsAt <= now && !x.IsDeleted).ToList();
        foreach (var subscription in expired)
        {
            if (subscription.Status == SubscriptionStatus.Active)
            {
                subscription.TrialStatus = TrialStatus.Expired;
                subscription.Touch();
                continue;
            }

            subscription.Status = SubscriptionStatus.Expired;
            subscription.TrialStatus = TrialStatus.Expired;
            subscription.Touch();

            var user = await _users.GetAsync(subscription.UserId, ct);
            if (user is not null && user.Plan != PlanType.Professional && user.Plan != PlanType.Enterprise)
            {
                user.Touch();
            }

            await _notifications.CreateForUserAsync(subscription.UserId, "Período de teste de 15 dias encerrado", "Seu teste de 15 dias terminou. Escolha um plano para continuar gerando propostas.", NotificationType.Warning, NotificationCategory.Plan, "/Subscription", "Conhecer planos", ct);
        }
        await _uow.SaveChangesAsync(ct);
        return expired.Count;
    }
}
