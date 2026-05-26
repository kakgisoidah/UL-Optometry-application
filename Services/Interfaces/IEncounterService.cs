// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IEncounterService.cs
//  Student encounter service — full lifecycle: draft → submit → resubmit.
//  Supervisor uses IReviewService (approve / revise).
//  Patient uses IPatientEncounterService (read-only).
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IEncounterService
{
    // ── Lists ─────────────────────────────────────────────────────────
    /// <summary>All encounters for the current student, newest first.</summary>
    Task<ApiResult<List<Encounter>>> GetMyEncountersAsync();

    /// <summary>Single encounter by ID (with attachments loaded).</summary>
    Task<ApiResult<Encounter>> GetEncounterByIdAsync(Guid encounterId);

    // ── Draft ─────────────────────────────────────────────────────────
    /// <summary>
    /// Create or update a draft. Status stays "Draft".
    /// Uses Upsert — safe to call at any step of the 6-step wizard.
    /// </summary>
    Task<ApiResult<Encounter>> SaveDraftAsync(Encounter encounter);

    // ── Submit ────────────────────────────────────────────────────────
    /// <summary>
    /// Submit encounter for supervisor review.
    /// Onsite  → status = "Submitted"  (goes to supervisor queue)
    /// Offsite → status = "Approved" + is_locked = true  (auto-approved)
    /// Also triggers a PoE entry insert for offsite encounters.
    /// </summary>
    Task<ApiResult<Encounter>> SubmitEncounterAsync(Encounter encounter);

    /// <summary>
    /// Resubmit after a supervisor requested revision.
    /// Clears supervisor_feedback, sets status back to "Submitted".
    /// </summary>
    Task<ApiResult<Encounter>> ResubmitEncounterAsync(Encounter encounter);

    // ── Booking prepopulation ──────────────────────────────────────────
    /// <summary>
    /// Build a partially-filled Encounter from a booking's linked data
    /// (patient name, file no, DOB, gender, date, cubicle, clinic, session).
    /// These fields should be rendered read-only in Step 1 of the form.
    /// </summary>
    Task<ApiResult<Encounter>> GetPrepopulatedFromBookingAsync(Guid bookingId);

    // ── Attachments ───────────────────────────────────────────────────
    /// <summary>
    /// Upload a file to Supabase Storage (encounter-attachments bucket)
    /// and insert a row in public.encounter_attachments.
    /// Returns the saved attachment record.
    /// </summary>
    Task<ApiResult<EncounterAttachment>> UploadAttachmentAsync(
        Guid encounterId,
        string fileName,
        Stream fileStream,
        string mimeType);

    /// <summary>Delete an attachment from Storage and the DB.</summary>
    Task<ApiResult<bool>> DeleteAttachmentAsync(Guid attachmentId, string storagePath);

    /// <summary>
    /// Get a short-lived signed URL for downloading an attachment.
    /// Valid for 60 seconds.
    /// </summary>
    Task<ApiResult<string>> GetAttachmentUrlAsync(string storagePath);
}
