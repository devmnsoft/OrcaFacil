using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Notification : Entity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationCategory Category { get; set; } = NotificationCategory.System;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public Guid? DocumentId { get; set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt ??= DateTime.UtcNow;
        Touch();
    }
}
