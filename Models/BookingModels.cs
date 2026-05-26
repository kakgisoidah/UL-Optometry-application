// ════════════════════════════════════════════════════════════════════════
//  Models/Booking/BookingModels.cs
//  All models related to the booking workflow.
//  Each class maps directly to a Supabase (PostgreSQL) table.
// ════════════════════════════════════════════════════════════════════════

using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using UL_Optometry.Constants;


namespace UL_Optometry.Models;
// ── Booking Status ────────────────────────────────────────────────────────
public enum BookingStatus
{
    Pending,
    Accepted,
    InProgress,
    Completed,
    Cancelled
}

// ── Clinic  →  public.clinics ─────────────────────────────────────────────
[Table("clinics")]
public class Clinic : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("weekday")]
    public int Weekday { get; set; }

    [Column("hex_color")]
    public string HexColor { get; set; } = "#3B82F6";

    // ── UI state — NOT in DB ──────────────────────────────────────────
    [Newtonsoft.Json.JsonIgnore]
    public bool IsSelected { get; set; }
}

// ── Cubicle  →  public.cubicles ───────────────────────────────────────────
[Table("cubicles")]
public class Cubicle : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

// ── Session  →  public.sessions ───────────────────────────────────────────
[Table("sessions")]
public partial class Session : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [Column("end_time")]
    public string EndTime { get; set; } = string.Empty;

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public string Display => $"{StartTime} – {EndTime}";
}

// ── Booking  →  public.bookings ───────────────────────────────────────────
[Table("bookings")]
public class Booking : BaseModel
{
    // DB primary key column is "booking_id", not "id"
    [PrimaryKey("booking_id", false)]
    public Guid Id { get; set; }

    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("clinic_id")]
    public int ClinicId { get; set; }

    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("booking_type")]
    public string BookingType { get; set; } = "Patient";

    [Column("booked_by_admin")]
    public Guid? BookedByAdmin { get; set; }

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public BookingStatus BookingStatus =>
        Enum.TryParse<BookingStatus>(Status, true, out var s)
            ? s : BookingStatus.Pending;

    [JsonIgnore]
    public bool CanCancel =>
    BookingStatus == BookingStatus.Pending ||
    BookingStatus == BookingStatus.Accepted ||
    Status == BookingStatuses.StudentAccepted;
    [JsonIgnore]
    public string DateDisplay =>
        Date.ToString("dd MMM yyyy");

    [JsonIgnore]
    public string StatusDisplay => Status switch
    {
        "Pending" => "Awaiting Confirmation",
        "Accepted" => "Confirmed – Awaiting Student",
        "StudentAccepted" => "Student Assigned – Awaiting Admin",
        "InProgress" => "In Progress",
        "Completed" => "Completed",
        "Cancelled" => "Cancelled",
        _ => Status
    };

    [JsonIgnore]
    public string BookingTypeDisplay => BookingType switch
    {
        "WalkIn" => "🚶 Walk-In",
        "FollowUp" => "🔄 Follow-Up",
        _ => "📱 Self-Booked"
    };

    // ── Joined fields (set by service — NOT in DB) ────────────────────
    [JsonIgnore] public string ClinicName { get; set; } = string.Empty;
    [JsonIgnore] public string PatientName { get; set; } = string.Empty;
    [JsonIgnore] public string SessionDisplay { get; set; } = string.Empty;
    [JsonIgnore] public string CubicleNumber { get; set; } = string.Empty;
    [JsonIgnore] public string SupervisorName { get; set; } = string.Empty;
    [JsonIgnore] public string StudentName { get; set; } = string.Empty;
}

// ── Booking Assignment  →  public.booking_assignments ────────────────────
[Table("booking_assignments")]
public class BookingAssignment : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("booking_id")]
    public Guid BookingId { get; set; }

    [Column("student_id")]
    public Guid? StudentId { get; set; }

    [Column("supervisor_id")]
    public Guid? SupervisorId { get; set; }

    [Column("cubicle_id")]
    public int? CubicleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }
}

// ── Request DTOs (not DB models — no BaseModel needed) ───────────────────
public class CreateBookingRequest
{
    public int ClinicId { get; set; }
    public int SessionId { get; set; }
    public DateTime Date { get; set; }
}

public class CancelBookingRequest
{
    public Guid BookingId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class AssignBookingRequest
{
    public Guid BookingId { get; set; }
    public Guid SupervisorId { get; set; }
    public Guid StudentId { get; set; }
    public int CubicleId { get; set; }
}