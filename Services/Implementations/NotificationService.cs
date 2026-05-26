// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/NotificationService.cs
// ════════════════════════════════════════════════════════════════════════

using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Models.Notification;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;
    private RealtimeChannel?         _channel;

    public NotificationService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    // ── Inbox ─────────────────────────────────────────────────────────
    public async Task<ApiResult<List<AppNotification>>> GetMyNotificationsAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<AppNotification>>.Ok(new());

            var result = await _supabase
                .From<AppNotification>()
                .Where(n => n.ToUserId == uid)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return ApiResult<List<AppNotification>>.Ok(result.Models);
        }
        catch (Exception ex)
        {
            return ApiResult<List<AppNotification>>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<bool>> MarkReadAsync(Guid notificationId)
    {
        try
        {
            await _supabase
                .From<AppNotification>()
                .Where(n => n.Id == notificationId)
                .Set(n => n.IsRead, true)
                .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> MarkAllReadAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<bool>.Ok(true);

            await _supabase
               .From<AppNotification>()
    .Where(n => n.ToUserId == uid)
    .Filter("is_read", Postgrest.Constants.Operator.Equals, "false")
    .Set(n => n.IsRead, true)
    .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Send ──────────────────────────────────────────────────────────
    public async Task<ApiResult<bool>> SendToUserAsync(
        Guid toUserId, string title, string message, string? type = null)
    {
        try
        {
            Guid.TryParse(_auth.CurrentUserId, out var fromId);
            var notif = new AppNotification
            {
                ToUserId     = toUserId,
                FromUserId   = fromId == Guid.Empty ? null : fromId,
                Title        = title,
                Message      = message,
                Type         = type,
            };
            await _supabase.From<AppNotification>().Insert(notif);
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> SendToRoleAsync(
        string role, string title, string message)
    {
        try
        {
            // Fetch all active users with this role
            var users = await _supabase
                .From<UserProfile>()
                .Where(p => p.RoleString == role && p.IsActive == true)
                .Get();

            Guid.TryParse(_auth.CurrentUserId, out var fromId);

            var notifications = users.Models.Select(u => new AppNotification
            {
                ToUserId   = u.UserId,
                FromUserId = fromId == Guid.Empty ? null : fromId,
                Title      = title,
                Message    = message,
                Type       = $"broadcast_{role}",
            }).ToList();

            if (notifications.Any())
                await _supabase.From<AppNotification>().Insert(notifications);

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> SendToAllAsync(string title, string message)
    {
        try
        {
            var users = await _supabase
                .From<UserProfile>()
                .Where(p => p.IsActive)
                .Get();

            Guid.TryParse(_auth.CurrentUserId, out var fromId);

            var notifications = users.Models.Select(u => new AppNotification
            {
                ToUserId   = u.UserId,
                FromUserId = fromId == Guid.Empty ? null : fromId,
                Title      = title,
                Message    = message,
                Type       = "broadcast_all",
            }).ToList();

            if (notifications.Any())
                await _supabase.From<AppNotification>().Insert(notifications);

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<AppNotification>>> GetSentAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<AppNotification>>.Ok(new());

            var result = await _supabase
                .From<AppNotification>()
                .Where(n => n.FromUserId == uid)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(50)
                .Get();

            return ApiResult<List<AppNotification>>.Ok(result.Models);
        }
        catch (Exception ex)
        {
            return ApiResult<List<AppNotification>>.Fail(ex.Message);
        }
    }

    //─────────────────────────────────────────────────────Realtime─────────────────────────────────────────────────────//─────────────
    public void SubscribeToRealtime(Action<AppNotification> onNewNotification)
    {
        if (!Guid.TryParse(_auth.CurrentUserId, out var uid)) return;

        _channel = _supabase.Realtime.Channel("notifications");

        _channel.Register(new PostgresChangesOptions("public", "notifications", PostgresChangesOptions.ListenType.Inserts, $"to_user_id=eq.{uid}"));

        _channel.AddPostgresChangeHandler(
            PostgresChangesOptions.ListenType.Inserts,
            (_, change) =>
            {
                try
                {
                    var notif = change.Model<AppNotification>();
                    if (notif is not null)
                        MainThread.BeginInvokeOnMainThread(
                            () => onNewNotification(notif));
                }
                catch { /* ignore parse errors */ }
            });

        _ = _channel.Subscribe();
    }

    public void UnsubscribeFromRealtime()
    {
        if (_channel is null) return;
        try { _ = _channel.Unsubscribe(); }
        catch { /* ignore */ }
        _channel = null;
    }
}
