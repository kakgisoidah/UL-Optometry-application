// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/INotificationService.cs
//  Shared notification inbox used by all portals.
//  Admin send methods are also here — they're gated by role in the VM.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models.Notification;

namespace UL_Optometry.Services.Interfaces;

public interface INotificationService
{
    // ── Inbox (all portals) ───────────────────────────────────────────
    /// <summary>
    /// Fetch the current user's notification inbox.
    /// RLS ensures only their own rows are returned.
    /// </summary>
    Task<ApiResult<List<AppNotification>>> GetMyNotificationsAsync();

    /// <summary>Mark a single notification as read.</summary>
    Task<ApiResult<bool>> MarkReadAsync(Guid notificationId);

    /// <summary>Mark all of the current user's notifications as read.</summary>
    Task<ApiResult<bool>> MarkAllReadAsync();

    // ── Send (Admin only — gated by role in AdminNotificationsViewModel) ──
    /// <summary>Send a notification to a specific user.</summary>
    Task<ApiResult<bool>> SendToUserAsync(Guid toUserId, string title, string message, string? type = null);

    /// <summary>
    /// Send to all users of a given role.
    /// Fetches all matching user IDs then inserts one row per user.
    /// </summary>
    Task<ApiResult<bool>> SendToRoleAsync(string role, string title, string message);

    /// <summary>Broadcast to every user in the system.</summary>
    Task<ApiResult<bool>> SendToAllAsync(string title, string message);

    /// <summary>Get all notifications sent by the current admin user.</summary>
    Task<ApiResult<List<AppNotification>>> GetSentAsync();

    // ── Realtime ──────────────────────────────────────────────────────
    /// <summary>
    /// Subscribe to new notifications for the current user via Supabase Realtime.
    /// Fires the callback each time a new notification row is inserted.
    /// </summary>
    void SubscribeToRealtime(Action<AppNotification> onNewNotification);

    /// <summary>Unsubscribe from Realtime when the page is not visible.</summary>
    void UnsubscribeFromRealtime();
}
