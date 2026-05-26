// ════════════════════════════════════════════════════════════════════════
//  Models/Admin/AdminModels.cs
//  Role-extension tables and admin-specific models.
// ════════════════════════════════════════════════════════════════════════

using Newtonsoft.Json;
using Postgrest.Attributes;



using Postgrest.Models;
using UL_Optometry.Models.Auth;

namespace UL_Optometry.Models.Admin;

// ── Blocked clinic date ───────────────────────────────────────────────────
[Table("blocked_dates")]
public class BlockedDate : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonIgnore]
    public string DateDisplay => Date.ToString("dd MMM yyyy");

    [JsonIgnore]
    public string DayName => Date.ToString("dddd");
}

// ── Admin book on behalf of patient request ───────────────────────────────
public class AdminBookForPatientRequest
{
    public Guid PatientId { get; set; }
    public int ClinicId { get; set; }
    public int SessionId { get; set; }
    public DateTime Date { get; set; }
    public string BookingType { get; set; } = "WalkIn";
    public Guid? BookedByAdmin { get; set; }
}

[Table("supervisors")]
public class SupervisorProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("op_number")]
    public string OpNumber { get; set; } = string.Empty;

    [Column("qualification")]
    public string Qualification { get; set; } = string.Empty;

    // ── Joined fields (populated by service — NOT real DB columns) ────
    [JsonIgnore] public string FullName { get; set; } = string.Empty;
    [JsonIgnore] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public bool IsActive { get; set; } = true;

    [JsonIgnore] public List<int> AssignedCubicleIds { get; set; } = new();
    [JsonIgnore] public List<string> AssignedCubicleNames { get; set; } = new();

    // ── Computed ──────────────────────────────────────────────────────
    [JsonIgnore]
    public string CubicleDisplay =>
        AssignedCubicleNames.Count == 0
            ? "Unassigned"
            : string.Join(", ", AssignedCubicleNames);

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

// ── Supervisor Cubicle  →  public.supervisor_cubicles ────────────────────
[Table("supervisor_cubicles")]
public class SupervisorCubicle : BaseModel
{
    [Column("supervisor_id")]
    public Guid SupervisorId { get; set; }

    [Column("cubicle_id")]
    public int CubicleId { get; set; }
}

// ── Student  →  public.students ──────────────────────────────────────────
[Table("students")]
public class StudentProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("student_number")]
    public string StudentNumber { get; set; } = string.Empty;

    [Column("year_of_study")]
    public int YearOfStudy { get; set; }

    // ── Joined fields (populated by service — NOT real DB columns) ────
    [JsonIgnore] public string FullName { get; set; } = string.Empty;
    [JsonIgnore] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public bool IsActive { get; set; } = true;
    [JsonIgnore] public double PoePercent { get; set; }

    // ── Computed ──────────────────────────────────────────────────────
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

    [JsonIgnore]
    public string PoeColor => PoePercent switch
    {
        >= 70 => "#22C55E",   // Success — on track
        >= 50 => "#F59E0B",   // Warning — needs attention
        _ => "#EF4444"    // Danger  — at risk
    };
}

// ── Patient  →  public.patients ──────────────────────────────────────────
[Table("patients")]
public class PatientDbProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("id_number")]
    public string? IdNumber { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("gender")]
    public string? Gender { get; set; }

    // ── Joined fields (populated by service — NOT real DB columns) ────
    [JsonIgnore] public string FullName { get; set; } = string.Empty;
    [JsonIgnore] public string Email { get; set; } = string.Empty;
    [JsonIgnore] public string Phone { get; set; } = string.Empty;
    [JsonIgnore] public bool IsActive { get; set; } = true;
}

// ── Audit Log  →  public.audit_logs ──────────────────────────────────────
[Table("audit_logs")]
public class AuditLog : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("entity")]
    public string Entity { get; set; } = string.Empty;

    [Column("entity_id")]
    public string? EntityId { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // ── Joined fields (populated by service — NOT real DB columns) ────
    [JsonIgnore] public string UserName { get; set; } = string.Empty;
    [JsonIgnore] public string UserRole { get; set; } = string.Empty;

    // ── Computed ──────────────────────────────────────────────────────
    [JsonIgnore]
    public string TimeDisplay =>
        CreatedAt.ToString("dd MMM yyyy HH:mm");
}

// ── Cubicle Assignment (not a DB table — scheduling display only) ─────────
public class CubicleAssignment
{
    public int CubicleId { get; set; }
    public string CubicleName { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid? SupervisorId { get; set; }
    public string SupervisorName { get; set; } = string.Empty;

    public bool HasStudent => StudentId.HasValue;
    public bool HasSupervisor => SupervisorId.HasValue;
}
