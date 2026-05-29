// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/ISchedulingService.cs
//  Admin scheduling — sessions, cubicles, and daily assignments.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Admin;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;

namespace UL_Optometry.Services.Interfaces;

public interface ISchedulingService
{
    // ── Sessions ──────────────────────────────────────────────────────
    Task<ApiResult<List<BlockedDate>>> GetBlockedDatesAsync();
    Task<ApiResult<BlockedDate>> BlockDateAsync(DateTime date, string reason);
    Task<ApiResult<bool>> UnblockDateAsync(int blockedDateId);
    /// <summary>All configured sessions (time slots). Max 3 per day.</summary>
    Task<ApiResult<List<Session>>> GetSessionsAsync();

    /// <summary>Add a new session. Returns error if 3 already exist.</summary>
    Task<ApiResult<Session>> AddSessionAsync(string name, string startTime, string endTime);

    /// <summary>Update a session's name or times.</summary>
    Task<ApiResult<Session>> UpdateSessionAsync(int sessionId, string name, string startTime, string endTime);

    /// <summary>
    /// Delete a session and all its cubicle assignments for that slot.
    /// Cannot delete if encounters are already submitted for it.
    /// </summary>
    Task<ApiResult<bool>> DeleteSessionAsync(int sessionId);

    // ── Cubicles ──────────────────────────────────────────────────────
    /// <summary>All active cubicles.</summary>
    Task<ApiResult<List<Cubicle>>> GetCubiclesAsync();

    /// <summary>Add a new cubicle (e.g. "C-9").</summary>
    Task<ApiResult<Cubicle>> AddCubicleAsync(string name);

    // ── Daily cubicle assignments ─────────────────────────────────────
    /// <summary>
    /// Get the full cubicle assignment grid for a specific session.
    /// Each row: cubicle + assigned student + assigned supervisor.
    /// </summary>
    Task<ApiResult<List<CubicleAssignment>>> GetAssignmentsAsync(int sessionId);

    /// <summary>
    /// Assign a student to a cubicle for a specific session.
    /// Replaces any existing student assignment for that cubicle/session.
    /// </summary>
    Task<ApiResult<bool>> AssignStudentAsync(int cubicleId, int sessionId, Guid studentUserId);

    /// <summary>Remove a student from a cubicle/session (returns to queue).</summary>
    Task<ApiResult<bool>> RemoveStudentAsync(int cubicleId, int sessionId);
    Task RemoveSupervisorAsync(int cubicleId, DateTime today);

    //suprvisor assignment
    Task<ApiResult<bool>> AssignSupervisorAsync(int cubicleId, Guid supervisorUserId, DateTime date);

    /// <summary>Get the supervisor assigned to a cubicle for a specific day.</summary>
    Task<ApiResult<List<SupervisorDailyAssignment>>> GetDailyAssignmentsAsync(DateTime date);
}
