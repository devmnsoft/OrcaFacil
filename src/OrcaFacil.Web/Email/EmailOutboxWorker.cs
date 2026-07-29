using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;
using OrcaFacil.Web.Security;

namespace OrcaFacil.Web.Email;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopes,
    IDataProtectionProvider protection,
    IOptions<EmailOutboxOptions> options,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private readonly string _instance = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    private readonly EmailOutboxOptions _options = options.Value;
    private DateTime _nextStuckRecovery = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EMAIL_OUTBOX_WORKER_STARTED Instance {Instance}", _instance);
        var consecutiveIdleCycles = 0;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(_options.ActivePollingSeconds);
                try
                {
                    await RecoverStuckMessagesWhenDueAsync(stoppingToken);
                    var processed = await ProcessBatchAsync(stoppingToken);
                    if (processed > 0)
                    {
                        consecutiveIdleCycles = 0;
                        delay = TimeSpan.FromSeconds(1);
                    }
                    else
                    {
                        consecutiveIdleCycles++;
                        var idleSeconds = Math.Min(_options.MaximumIdlePollingSeconds,
                            _options.IdlePollingSeconds + ((consecutiveIdleCycles - 1) * 5));
                        delay = TimeSpan.FromSeconds(idleSeconds);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "EMAIL_OUTBOX_CYCLE_FAILED");
                    delay = TimeSpan.FromSeconds(Math.Min(_options.MaximumIdlePollingSeconds,
                        _options.ActivePollingSeconds * Math.Pow(2, Math.Min(consecutiveIdleCycles + 1, 4))));
                }

                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            logger.LogInformation("EMAIL_OUTBOX_WORKER_STOPPED Instance {Instance}", _instance);
        }
    }

    private async Task RecoverStuckMessagesWhenDueAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (now < _nextStuckRecovery) return;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
        var count = await db.EmailOutboxMessages
            .Where(x => x.Status == EmailOutboxStatus.Processing &&
                        x.ProcessingStartedAt < now.AddMinutes(-_options.StuckRecoveryIntervalMinutes))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, EmailOutboxStatus.Pending)
                .SetProperty(x => x.ProcessingInstanceId, (string?)null)
                .SetProperty(x => x.ProcessingStartedAt, (DateTime?)null), cancellationToken);
        _nextStuckRecovery = now.AddMinutes(_options.StuckRecoveryIntervalMinutes);
        if (count > 0)
            logger.LogInformation("EMAIL_OUTBOX_STUCK_MESSAGES_RECOVERED Count {Count}", count);
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrcaFacilDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var now = DateTime.UtcNow;
        var ids = await db.EmailOutboxMessages.AsNoTracking()
            .Where(x => (x.Status == EmailOutboxStatus.Pending || x.Status == EmailOutboxStatus.Failed) &&
                        x.NextAttemptAt <= now && x.Attempts < _options.MaximumAttempts)
            .OrderBy(x => x.Priority).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => x.Id).Take(_options.BatchSize).ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var id in ids)
        {
            // The conditional UPDATE is the atomic claim: competing instances cannot both transition the same row.
            var claimed = await db.EmailOutboxMessages
                .Where(x => x.Id == id && (x.Status == EmailOutboxStatus.Pending || x.Status == EmailOutboxStatus.Failed) &&
                            x.NextAttemptAt <= now && x.Attempts < _options.MaximumAttempts)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, EmailOutboxStatus.Processing)
                    .SetProperty(x => x.ProcessingStartedAt, now)
                    .SetProperty(x => x.ProcessingInstanceId, _instance)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1), cancellationToken);
            if (claimed == 0) continue;

            processed++;
            logger.LogInformation("EMAIL_OUTBOX_MESSAGE_CLAIMED Id {Id} Instance {Instance}", id, _instance);
            var item = await db.EmailOutboxMessages.SingleAsync(x => x.Id == id, cancellationToken);
            await DeliverAsync(item, sender, cancellationToken);
            item.ProcessingInstanceId = null;
            item.ProcessingStartedAt = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        return processed;
    }

    private async Task DeliverAsync(EmailOutboxMessage item, IEmailSender sender, CancellationToken cancellationToken)
    {
        try
        {
            var recipient = protection.CreateProtector("OrcaFacil.EmailOutbox.Recipient.v1").Unprotect(item.ProtectedRecipient);
            var json = protection.CreateProtector("OrcaFacil.EmailOutbox.Payload.v1").Unprotect(item.ProtectedPayload!);
            var payload = JsonSerializer.Deserialize<OutboxPayload>(json)!;
            if (payload.ExpiresAt <= DateTime.UtcNow)
            {
                DeadLetter(item, "CONTENT_EXPIRED");
                return;
            }

            var result = await sender.SendAsync(new EmailMessage(new EmailAddress(recipient), payload.Subject, payload.Text, payload.Html), cancellationToken);
            if (result.Succeeded)
            {
                item.Status = EmailOutboxStatus.Sent;
                item.SentAt = DateTime.UtcNow;
                item.ProtectedPayload = null;
                item.ProtectedRecipient = string.Empty;
                logger.LogInformation("EMAIL_OUTBOX_MESSAGE_SENT Id {Id} CorrelationId {CorrelationId} Recipient {Recipient}",
                    item.Id, item.CorrelationId, item.RecipientMasked);
            }
            else Fail(item, result.Code);
        }
        catch (Exception exception)
        {
            logger.LogWarning("EMAIL_OUTBOX_MESSAGE_TRANSIENT_FAILURE Id {Id} Type {Type}", item.Id, exception.GetType().Name);
            Fail(item, "PROCESSING_FAILED");
        }
    }

    private void Fail(EmailOutboxMessage item, string? code)
    {
        item.LastErrorCode = code;
        if (item.Attempts >= _options.MaximumAttempts) DeadLetter(item, code);
        else
        {
            item.Status = EmailOutboxStatus.Failed;
            item.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, item.Attempts) * 15);
        }
    }

    private void DeadLetter(EmailOutboxMessage item, string? code)
    {
        item.Status = EmailOutboxStatus.DeadLetter;
        item.DeadLetteredAt = DateTime.UtcNow;
        item.LastErrorCode = code;
        logger.LogInformation("EMAIL_OUTBOX_MESSAGE_DEAD_LETTER Id {Id} Code {Code}", item.Id, code);
    }
}
