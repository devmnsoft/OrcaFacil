using OrcaFacil.Application.Notifications;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Application.Abstractions;

public interface INotificationService
{
    Task<Guid> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default);
    Task<Guid> CreateForUserAsync(Guid userId, string title, string message, NotificationType type = NotificationType.Info, NotificationCategory category = NotificationCategory.System, string? actionUrl = null, string? actionText = null, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationListItemDto>> ListUserNotificationsAsync(Guid userId, int take = 50, CancellationToken ct = default);
}
