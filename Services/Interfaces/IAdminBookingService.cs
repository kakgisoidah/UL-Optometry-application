// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IAdminBookingService.cs
//  Admin booking management — view all, assign, auto-assign, cancel.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Common;

namespace UL_Optometry.Services.Interfaces;

public interface IAdminBookingService
{
    // ── Lists ─────────────────────────────────────────────────────────
    /// <summary>
    /// All bookings across all patients.
    /// Pass a status string to filter (e.g. "Pending").
    /// Pass null for all statuses.
    /// </summary>
    Task<ApiResult<List<Booking>>> GetAllBookingsAsync(string? status = null);

    /// <summary>Single booking by ID with all joined fields.</summary>
    Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId);

    // ── Assign ────────────────────────────────────────────────────────
    /// <summary>
    /// Manually assign a supervisor + student + cubicle to a booking.
    /// Inserts/updates booking_assignments.
    /// Sets booking status = "Accepted".
    /// </summary>
    Task<ApiResult<Booking>> AssignBookingAsync(AssignBookingRequest request);

    /// <summary>
    /// Auto-assign all Pending bookings by distributing across
    /// available supervisors and their cubicles in round-robin order.
    /// Returns count of bookings assigned.
    /// </summary>
    Task<ApiResult<int>> AutoAssignAllPendingAsync();

    // ── Cancel ────────────────────────────────────────────────────────
    /// <summary>Admin cancels any booking regardless of status.</summary>
    Task<ApiResult<bool>> CancelBookingAsync(Guid bookingId, string reason);

    /// <summary>Admin creates a booking on behalf of a patient.</summary>
    Task<ApiResult<Booking>> BookForPatientAsync(AdminBookForPatientRequest request);
}
