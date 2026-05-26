// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IPoeService.cs
//  Portfolio of Evidence — 500 hours across 5 categories.
//  Used by StudentPoePage and AdminPoePage.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IPoeService
{
    // ── Student ───────────────────────────────────────────────────────
    /// <summary>
    /// Full PoE summary for the current student.
    /// Aggregates poe_entries by category, computes logged vs required hours.
    /// Total required = 500 hours.
    /// </summary>
    Task<ApiResult<PoeSummary>> GetPoeSummaryAsync();

    /// <summary>
    /// Recent PoE entries for the current student, newest first.
    /// Used in the "Recent Encounters" list on StudentPoePage.
    /// </summary>
    Task<ApiResult<List<RecentPoeEncounter>>> GetRecentEntriesAsync(int limit = 10);

    /// <summary>All 5 PoE categories with required hours and hex colours.</summary>
    Task<ApiResult<List<PoeCategory>>> GetCategoriesAsync();

    // ── Admin ─────────────────────────────────────────────────────────
    /// <summary>
    /// PoE summary for a specific student.
    /// Used on AdminStudentDetailPage.
    /// </summary>
    Task<ApiResult<PoeSummary>> GetPoeSummaryForStudentAsync(Guid studentUserId);

    /// <summary>
    /// Average PoE % across all active students.
    /// Used on AdminPoePage stat cards.
    /// </summary>
    Task<ApiResult<double>> GetAveragePoePercentAsync();

    /// <summary>
    /// All students sorted by PoE %, ascending (at-risk first).
    /// Used on AdminPoePage.
    /// </summary>
    Task<ApiResult<List<(Guid UserId, string FullName, double PoePercent)>>>
        GetAllStudentPoeAsync();
}
