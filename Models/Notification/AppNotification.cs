// ════════════════════════════════════════════════════════════════════════
//  Models/Notification/AppNotification.cs
//  Maps to public.notifications in Supabase.
//  Used by all four portals — each user sees only their own notifications
//  enforced by RLS (to_user_id = auth.uid()).
// ════════════════════════════════════════════════════════════════════════

using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;
namespace UL_Optometry.Models.Notification;


[Table("notifications")]
public class AppNotification : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("to_user_id")]
    public Guid ToUserId { get; set; }

    [Column("from_user_id")]
    public Guid? FromUserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - CreatedAt.ToUniversalTime();
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
            return CreatedAt.ToString("dd MMM yyyy");
        }
    }
}

// ── Admin send request (not a DB model) ──────────────────────────────────
public class SendNotificationRequest
{
    public string RecipientType { get; set; } = "all";
    public Guid? ToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
}
