using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.Notifications;
using OrcaFacil.Web.Extensions;

namespace OrcaFacil.Web.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUserService _currentUser;

    public IndexModel(INotificationService notifications, ICurrentUserService currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public IReadOnlyList<NotificationListItemDto> Items { get; private set; } = [];
    public int UnreadCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Items = await _notifications.ListUserNotificationsAsync(_currentUser.UserId, 100, ct);
        UnreadCount = await _notifications.GetUnreadCountAsync(_currentUser.UserId, ct);
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(Guid id, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(id, _currentUser.UserId, ct);
        TempData.Success("Notificação marcada como lida.");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync(CancellationToken ct)
    {
        await _notifications.MarkAllAsReadAsync(_currentUser.UserId, ct);
        TempData.Success("Todas as notificações foram marcadas como lidas.");
        return RedirectToPage();
    }
}
