// ════════════════════════════════════════════════════════════════════════
//  Models/Encounter/EncounterModels.cs
//  Encounter is the core clinical record.
//  All 5 sections are flat columns in public.encounters — no child tables
//  (except attachments which have their own table).
// ════════════════════════════════════════════════════════════════════════

using Postgrest.Attributes;
using Newtonsoft.Json;
using Postgrest.Models;

namespace UL_Optometry.Models;

// ── Schedule Item (UI-only, not a DB model) ───────────────────────────────
public class ScheduleItem
{
    public string TimeRange    { get; set; } = string.Empty;
    public string CubicleName  { get; set; } = string.Empty;
    public string ClinicName   { get; set; } = string.Empty;
    public string StudentName  { get; set; } = string.Empty;
}

// ── Encounter Status ──────────────────────────────────────────────────────
public enum EncounterStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Revision
}

// ── Encounter  →  public.encounters ──────────────────────────────────────
[Table("encounters")]
public class Encounter : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("booking_id")]
    public Guid? BookingId { get; set; }

    [Column("session_id")]
    public int? SessionId { get; set; }

    [Column("cubicle_id")]
    public int? CubicleId { get; set; }

    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Column("supervisor_id")]
    public Guid? SupervisorId { get; set; }

    [Column("encounter_type")]
    public string EncounterType { get; set; } = "Onsite";

    [Column("status")]
    public string Status { get; set; } = "Draft";

    [Column("is_locked")]
    public bool IsLocked { get; set; }

    [Column("supervisor_feedback")]
    public string? SupervisorFeedback { get; set; }

    [Column("signed_off_at")]
    public DateTime? SignedOffAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // ── Section 1 — Patient & Exam Info ──────────────────────────────
    [Column("patient_file_number")]
    public string PatientFileNumber { get; set; } = string.Empty;

    [Column("patient_name")]
    public string PatientName { get; set; } = string.Empty;

    [Column("dob")]
    public string Dob { get; set; } = string.Empty;

    [Column("gender")]
    public string Gender { get; set; } = string.Empty;

    [Column("exam_type")]
    public string ExamType { get; set; } = string.Empty;

    [Column("encounter_time")]
    public string EncounterTime { get; set; } = string.Empty;

    [Column("location")]
    public string Location { get; set; } = string.Empty;

    [Column("reason_for_visit")]
    public string ReasonForVisit { get; set; } = string.Empty;

    [Column("history")]
    public string History { get; set; } = string.Empty;

    [Column("ocular_history")]
    public string OcularHistory { get; set; } = string.Empty;

    [Column("offsite_supervisor_name")]
    public string OffSiteSupervisorName { get; set; } = string.Empty;

    [Column("offsite_op_number")]
    public string OffSiteOpNumber { get; set; } = string.Empty;

    // ── Section 2 — Clinical Findings ────────────────────────────────
    [Column("distance_va_od")]
    public string DistanceVaOD { get; set; } = string.Empty;

    [Column("distance_va_os")]
    public string DistanceVaOS { get; set; } = string.Empty;

    [Column("near_va_od")]
    public string NearVaOD { get; set; } = string.Empty;

    [Column("near_va_os")]
    public string NearVaOS { get; set; } = string.Empty;

    [Column("refraction_od")]
    public string RefractionOD { get; set; } = string.Empty;

    [Column("refraction_os")]
    public string RefractionOS { get; set; } = string.Empty;

    [Column("npc_break")]
    public string NpcBreak { get; set; } = string.Empty;

    [Column("npc_recovery")]
    public string NpcRecovery { get; set; } = string.Empty;

    [Column("cover_test_distance")]
    public string CoverTestDistance { get; set; } = string.Empty;

    [Column("cover_test_near")]
    public string CoverTestNear { get; set; } = string.Empty;

    [Column("anterior_segment")]
    public string AnteriorSegment { get; set; } = string.Empty;

    [Column("posterior_segment")]
    public string PosteriorSegment { get; set; } = string.Empty;

    [Column("additional_findings")]
    public string AdditionalFindings { get; set; } = string.Empty;

    // ── Section 3 — Impression & Plan ────────────────────────────────
    [Column("diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;

    [Column("differential_diagnosis")]
    public string DifferentialDiagnosis { get; set; } = string.Empty;

    [Column("management_plan")]
    public string ManagementPlan { get; set; } = string.Empty;

    [Column("referral")]
    public string Referral { get; set; } = string.Empty;

    [Column("follow_up")]
    public string FollowUp { get; set; } = string.Empty;

    [Column("treatment_notes")]
    public string TreatmentNotes { get; set; } = string.Empty;

    // ── Section 4 — Reflection & PoE ─────────────────────────────────
    [Column("student_reflection")]
    public string StudentReflection { get; set; } = string.Empty;

    [Column("poe_categories")]
    public string PoeCategoriesJson { get; set; } = "[]";

    [Column("confirm_accurate")]
    public bool ConfirmAccurate { get; set; }

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public EncounterStatus EncounterStatus =>
        Enum.TryParse<EncounterStatus>(Status, true, out var s)
            ? s : EncounterStatus.Draft;

    [JsonIgnore]
    public bool IsOffsite => EncounterType == "Offsite";

    [JsonIgnore]
    public bool NeedsRevision => EncounterStatus == EncounterStatus.Revision;

    [JsonIgnore]
    public bool IsApproved => EncounterStatus == EncounterStatus.Approved;

    [JsonIgnore]
    public bool CanEdit =>
        !IsLocked &&
        (EncounterStatus == EncounterStatus.Draft ||
         EncounterStatus == EncounterStatus.Revision);

    [JsonIgnore]
    public string StatusDisplay => EncounterStatus switch
    {
        EncounterStatus.Draft => "Draft",
        EncounterStatus.Submitted => "Submitted",
        EncounterStatus.UnderReview => "Under Review",
        EncounterStatus.Approved => "Approved",
        EncounterStatus.Revision => "Revision Requested",
        _ => Status
    };

    [JsonIgnore]
    public string DateDisplay =>
        CreatedAt.ToString("dd MMM yyyy");

    // ── Section 5 — Attachments (loaded separately) ───────────────────
    // Not a DB column — populated by service after separate query
    [JsonIgnore]
    public List<EncounterAttachment> Attachments { get; set; } = new();

    // ── Joined fields (set by service — NOT in DB) ────────────────────
    [JsonIgnore] public string StudentName { get; set; } = string.Empty;
    [JsonIgnore] public string StudentInitials { get; set; } = string.Empty;
    [JsonIgnore] public string SupervisorName { get; set; } = string.Empty;
    [JsonIgnore] public string ClinicName { get; set; } = string.Empty;
    [JsonIgnore] public string SlotDisplay { get; set; } = string.Empty;
    [JsonIgnore] public string CubicleNumber { get; set; } = string.Empty;
}

// ── Encounter Attachment  →  public.encounter_attachments ────────────────
[Table("encounter_attachments")]
public class EncounterAttachment : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("encounter_id")]
    public Guid EncounterId { get; set; }

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("storage_path")]
    public string StoragePath { get; set; } = string.Empty;

    [Column("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    // ── Computed — NOT in DB ──────────────────────────────────────────
    [JsonIgnore]
    public string SizeDisplay =>
        SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:F1} KB"
            : $"{SizeBytes / (1024.0 * 1024):F1} MB";
}

// ── Request models (not DB models) ───────────────────────────────────────
public class ApproveEncounterRequest
{
    public Guid EncounterId { get; set; }
    public string SupervisorOpNumber { get; set; } = string.Empty;
}

public class RevisionRequest
{
    public Guid EncounterId { get; set; }
    public string Feedback { get; set; } = string.Empty;
}
