using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notifications;
    private readonly IUnitOfWork _uow;

    public NotificationService(IRepository<Notification> notifications, IUnitOfWork uow)
    {
        _notifications = notifications;
        _uow = uow;
    }

    public async Task<Guid> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
        => await CreateForUserAsync(request.UserId, request.Title, request.Message, request.Type, request.Category, request.ActionUrl, request.ActionText, ct);

    public async Task<Guid> CreateForUserAsync(Guid userId, string title, string message, NotificationType type = NotificationType.Info, NotificationCategory category = NotificationCategory.System, string? actionUrl = null, string? actionText = null, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type,
            Category = category,
            ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim(),
            ActionText = string.IsNullOrWhiteSpace(actionText) ? null : actionText.Trim()
        };

        await _notifications.AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);
        return notification.Id;
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = _notifications.Query().SingleOrDefault(x => x.Id == notificationId && x.UserId == userId && !x.IsDeleted);
        if (notification is null || notification.IsRead) return;
        notification.MarkAsRead();
        await _uow.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var notification in _notifications.Query().Where(x => x.UserId == userId && !x.IsRead && !x.IsDeleted)) notification.MarkAsRead();
        await _uow.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_notifications.Query().Count(x => x.UserId == userId && !x.IsRead && !x.IsDeleted));

    public Task<IReadOnlyList<NotificationListItemDto>> ListUserNotificationsAsync(Guid userId, int take = 50, CancellationToken ct = default)
    {
        var items = _notifications.Query()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(x => new NotificationListItemDto(x.Id, x.Title, x.Message, x.Type.ToString(), x.Category.ToString(), x.ActionUrl, x.ActionText, x.IsRead, x.ReadAt, x.CreatedAt))
            .ToList();
        return Task.FromResult<IReadOnlyList<NotificationListItemDto>>(items);
    }
}

public sealed record CreateNotificationRequest(Guid UserId, string Title, string Message, NotificationType Type, NotificationCategory Category, string? ActionUrl = null, string? ActionText = null);
public sealed record NotificationListItemDto(Guid Id, string Title, string Message, string Type, string Category, string? ActionUrl, string? ActionText, bool IsRead, DateTime? ReadAt, DateTime CreatedAt);
