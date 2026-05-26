// ════════════════════════════════════════════════════════════════════════
//  Models/Auth/UserProfile.cs
//  Maps to public.profiles in Supabase.
//  One-to-one with auth.users via user_id.
// ════════════════════════════════════════════════════════════════════════
using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using UL_Optometry.Models.Auth;

namespace UL_Optometry.Models;


[Table("profiles")]
public class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Column("role")]
    public string RoleString { get; set; } = "patient";

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // ── Computed — NOT in DB — must be [JsonIgnore] ───────────────────
    [JsonIgnore]
    public UserRole Role =>
        UserRoleExtensions.FromDbString(RoleString);

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = FullName.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}"
                : parts.Length == 1
                    ? char.ToUpper(parts[0][0]).ToString()
                    : "?";
        }
    }
}