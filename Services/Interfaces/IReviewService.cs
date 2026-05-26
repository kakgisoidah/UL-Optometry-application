// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IReviewService.cs
//  Supervisor encounter review workflow.
//  Approve = permanent lock. Revision = returns to student with feedback.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IReviewService
{
    // ── Queue tabs ────────────────────────────────────────────────────
    /// <summary>
    /// Encounters assigned to this supervisor's cubicles with
    /// status = "Submitted" or "UnderReview". Newest first.
    /// </summary>
    Task<ApiResult<List<Encounter>>> GetPendingQueueAsync();

    /// <summary>
    /// Encounters this supervisor reviewed today
    /// (signed off or returned for revision today).
    /// </summary>
    Task<ApiResult<List<Encounter>>> GetReviewedTodayAsync();

    /// <summary>
    /// All encounters approved by this supervisor.
    /// Status = "Approved". Newest first.
    /// </summary>
    Task<ApiResult<List<Encounter>>> GetAllSignedOffAsync();

    // ── Detail ────────────────────────────────────────────────────────
    /// <summary>
    /// Full encounter for review — all 5 sections plus attachments.
    /// Also sets status = "UnderReview" while the supervisor has it open.
    /// </summary>
    Task<ApiResult<Encounter>> GetEncounterForReviewAsync(Guid encounterId);

    // ── Actions ───────────────────────────────────────────────────────
    /// <summary>
    /// Approve and permanently lock the encounter.
    /// Sets status = "Approved", is_locked = true, signed_off_at = now().
    /// Inserts PoE entries for each selected category.
    /// NO ONE can edit a locked encounter — enforced by RLS.
    /// </summary>
    Task<ApiResult<Encounter>> ApproveEncounterAsync(ApproveEncounterRequest request);

    /// <summary>
    /// Return the encounter to the student for corrections.
    /// Sets status = "Revision", supervisor_feedback = feedback text.
    /// Student sees the feedback and an Edit and Resubmit button.
    /// </summary>
    Task<ApiResult<Encounter>> RequestRevisionAsync(RevisionRequest request);

    // ── PDF ───────────────────────────────────────────────────────────
    /// <summary>
    /// Get a signed URL to download the signed-off encounter as PDF.
    /// PDF is generated server-side by a Supabase Edge Function.
    /// </summary>
    Task<ApiResult<string>> GetPdfUrlAsync(Guid encounterId);

    // ── Dashboard helpers ─────────────────────────────────────────────
    /// <summary>Today's schedule: bookings in the supervisor's cubicles for today.</summary>
    Task<ApiResult<List<ScheduleItem>>> GetTodayScheduleAsync();

    /// <summary>Number of cubicles currently assigned to this supervisor.</summary>
    Task<ApiResult<int>> GetAssignedCubiclesCountAsync();

    /// <summary>Returns the OP number of the currently signed-in supervisor.</summary>
    Task<ApiResult<string>> GetCurrentSupervisorOpNumberAsync();
}
