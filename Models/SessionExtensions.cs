// ════════════════════════════════════════════════════════════════════════
//  Models/SessionExtensions.cs
//  Admin display helpers added to the Session partial class.
// ════════════════════════════════════════════════════════════════════════

using Newtonsoft.Json;

namespace UL_Optometry.Models;

public partial class Session
{
    // ── "Session 1 (08:00–10:00)" — shown on the tab strip ───────────
    [JsonIgnore]
    public string TabLabel => $"{Name} ({Display})";

    // ── Duration derived from StartTime / EndTime stored in Display ───
    // Display is expected to be "HH:mm – HH:mm" (set by the service).
    [JsonIgnore]
    public string Duration
    {
        get
        {
            var parts = Display?.Split('–');
            if (parts?.Length >= 2 &&
                TimeSpan.TryParse(parts[0].Trim(), out var start) &&
                TimeSpan.TryParse(parts[1].Trim(), out var end))
            {
                var diff = end - start;
                return diff.TotalMinutes % 60 == 0
                    ? $"{(int)diff.TotalHours}hr{(diff.TotalHours == 1 ? "" : "s")}"
                    : $"{(int)diff.TotalHours}hr {diff.Minutes}min";
            }
            return "—";
        }
    }

    // ── Static status — all active sessions are "Active" ─────────────
    [JsonIgnore]
    public string Status => "Active";
}
