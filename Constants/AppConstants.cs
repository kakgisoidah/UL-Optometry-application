// ════════════════════════════════════════════════════════════════════════
//  Constants/AppConstants.cs
//  Application-wide constants shared by all portals.
// ════════════════════════════════════════════════════════════════════════

namespace UL_Optometry.Constants;
using Supabase;

// ── PoE ──────────────────────────────────────────────────────────────────
public static class PoeConstants
{
    public const int TotalRequiredHours = 500;

    // Category names — must match exactly what is stored in the DB
    public const string BinopularVision = "Binocular Vision";
    public const string Paediatric = "Paediatric";
    public const string ContactLens = "Contact Lens";
    public const string LowVision = "Low Vision";
    public const string OcularDisease = "Ocular Disease";

    // Required hours per category
    public const int BinopularVisionHours = 150;
    public const int PaediatricHours = 100;
    public const int ContactLensHours = 100;
    public const int LowVisionHours = 75;
    public const int OcularDiseaseHours = 75;

    // Hex colours (match AppColors.xaml)
    public const string BinopularVisionColor = "#3B82F6";
    public const string PaediatricColor = "#22C55E";
    public const string ContactLensColor = "#F59E0B";
    public const string LowVisionColor = "#8B5CF6";
    public const string OcularDiseaseColor = "#EC4899";

    // Hours credited per completed encounter
    public const double HoursPerEncounter = 1.5;
}

// ── Clinic Schedule ───────────────────────────────────────────────────────
public static class ClinicConstants
{
    // Weekday → clinic name (Mon=1 … Fri=5)
    public static readonly IReadOnlyDictionary<int, string> ClinicByWeekday =
        new Dictionary<int, string>
        {
            { 1, "Severely Reduced Vision" },   // Monday
            { 2, "Paediatric" },                // Tuesday
            { 3, "Binocular Vision" },           // Wednesday
            { 4, "Contact Lens" },               // Thursday
            { 5, "General Consultation" },       // Friday
        };

    // Clinic name → hex colour
    public static readonly IReadOnlyDictionary<string, string> ClinicColors =
        new Dictionary<string, string>
        {
            { "Severely Reduced Vision", "#8B5CF6" },
            { "Paediatric",              "#EC4899" },
            { "Binocular Vision",        "#3B82F6" },
            { "Contact Lens",            "#F59E0B" },
            { "General Consultation",    "#22C55E" },
        };
}

// ── Time Slots ────────────────────────────────────────────────────────────
public static class SlotConstants
{
    public const string Slot1Display = "08:30 – 10:00";
    public const string Slot2Display = "10:00 – 11:30";
    public const string Slot3Display = "11:30 – 13:00";

    public static readonly IReadOnlyList<string> AllSlots = new[]
    {
        Slot1Display, Slot2Display, Slot3Display
    };
}

// ── Supabase Table Names ──────────────────────────────────────────────────
public static class SupabaseTables
{
    public const string Profiles = "profiles";
    public const string Supervisors = "supervisors";
    public const string SupervisorCubicles = "supervisor_cubicles";
    public const string Students = "students";
    public const string Patients = "patients";
    public const string Clinics = "clinics";
    public const string Cubicles = "cubicles";
    public const string Sessions = "sessions";
    public const string Bookings = "bookings";
    public const string BookingAssignments = "booking_assignments";
    public const string Encounters = "encounters";
    public const string EncounterAttachments = "encounter_attachments";
    public const string PoeCategories = "poe_categories";
    public const string PoeEntries = "poe_entries";
    public const string Notifications = "notifications";
    public const string AuditLogs = "audit_logs";
}

// ── Supabase Storage Buckets ──────────────────────────────────────────────
public static class SupabaseBuckets
{
    public const string EncounterAttachments = "encounter-attachments";
}

// ── Roles ─────────────────────────────────────────────────────────────────
public static class Roles
{
    public const string Admin = "admin";
    public const string Supervisor = "supervisor";
    public const string Student = "student";
    public const string Patient = "patient";
}

// ── Encounter Types ───────────────────────────────────────────────────────
public static class EncounterTypes
{
    public const string Onsite = "Onsite";
    public const string Offsite = "Offsite";
}

// ── Booking Statuses ──────────────────────────────────────────────────────
public static class BookingStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string StudentAccepted = "StudentAccepted";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

// ── Encounter Statuses ────────────────────────────────────────────────────
public static class EncounterStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Revision = "Revision";
}