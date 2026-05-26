// ════════════════════════════════════════════════════════════════════════
//  Models/PoE/PoeModels.cs
//  Portfolio of Evidence — 500 hours across 5 categories.
// ════════════════════════════════════════════════════════════════════════

using Postgrest.Attributes;
using Postgrest.Models;
using Newtonsoft.Json;
namespace UL_Optometry.Models;
// ── PoE Category  →  public.poe_categories ───────────────────────────────
[Table("poe_categories")]
public class PoeCategory : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("required_hours")]
    public int RequiredHours { get; set; }

    [Column("hex_color")]
    public string HexColor { get; set; } = "#3B82F6";

    // ── Runtime (set by service — NOT in DB) ─────────────────────────
    [JsonIgnore] public double LoggedHours { get; set; }

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public double PercentComplete =>
        RequiredHours == 0 ? 0
        : Math.Min(Math.Round(LoggedHours / RequiredHours * 100, 1), 100);

    [JsonIgnore]
    public bool IsComplete => LoggedHours >= RequiredHours;

    [JsonIgnore]
    public double RemainingHours =>
        Math.Max(RequiredHours - LoggedHours, 0);

    [JsonIgnore]
    public double ProgressFraction =>
        RequiredHours == 0 ? 0
        : Math.Min(LoggedHours / RequiredHours, 1.0);
}

// ── PoE Entry  →  public.poe_entries ─────────────────────────────────────
[Table("poe_entries")]
public class PoeEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("encounter_id")]
    public Guid EncounterId { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("hours")]
    public double Hours { get; set; } = 1.5;

    [Column("credited_at")]
    public DateTime CreditedAt { get; set; }

    // ── Joined fields (set by service — NOT in DB) ────────────────────
    [JsonIgnore] public string CategoryName { get; set; } = string.Empty;
    [JsonIgnore] public string ClinicName { get; set; } = string.Empty;
}

// ── PoE Summary (computed — not stored in DB) ────────────────────────────
public class PoeSummary
{
    public const int TotalRequired = 500;

    public double TotalLogged { get; set; }
    public List<PoeCategory> Categories { get; set; } = new();

    [JsonIgnore]
    public double OverallPercent =>
        Math.Round(Math.Min(TotalLogged / TotalRequired * 100, 100), 1);

    [JsonIgnore]
    public double Remaining =>
        Math.Max(TotalRequired - TotalLogged, 0);

    [JsonIgnore]
    public bool IsComplete => TotalLogged >= TotalRequired;
}

// ── Recent PoE Encounter (lightweight list item) ──────────────────────────
public class RecentPoeEncounter
{
    public string ClinicName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double Hours { get; set; }

    [JsonIgnore]
    public string DateDisplay => Date.ToString("dd MMM yyyy");

    [JsonIgnore]
    public string HoursDisplay => $"+{Hours:F1} hrs";
}

// ── Category selection checkbox (used in EncounterFormViewModel) ──────────
public partial class PoeCategorySelection
    : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private bool _isSelected;

    public string Name { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string HexColor { get; init; } = "#3B82F6";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}