// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IQueueService.cs
//  Student booking queue — accept, cancel, and status views.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;

namespace UL_Optometry.Services.Interfaces;

public interface IQueueService
{
    // ── Queue tabs ────────────────────────────────────────────────────
    /// <summary>
    /// Bookings that are available for the current student to accept.
    /// Status = "Pending" and not yet assigned to any student.
    /// </summary>
    Task<ApiResult<List<Booking>>> GetQueueAsync();

    /// <summary>Bookings this student has accepted (status = "Accepted").</summary>
    Task<ApiResult<List<Booking>>> GetAcceptedAsync();

    /// <summary>Bookings currently in progress (status = "InProgress").</summary>
    Task<ApiResult<List<Booking>>> GetInProgressAsync();

    /// <summary>Completed bookings for this student (status = "Completed").</summary>
    Task<ApiResult<List<Booking>>> GetCompletedAsync();

    /// <summary>Single booking by ID (with joined fields).</summary>
    Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId);

    // ── Actions ───────────────────────────────────────────────────────
    /// <summary>
    /// Student accepts a booking from the queue.
    /// Sets status = "Accepted" and records the student_id
    /// in booking_assignments.
    /// A student cannot decline — they can only accept or leave
    /// the booking in the queue.
    /// </summary>
    Task<ApiResult<Booking>> AcceptBookingAsync(Guid bookingId);

    /// <summary>
    /// Cancel an accepted booking before starting the encounter form.
    /// Sets status back to "Pending" and clears the student assignment.
    /// </summary>
    Task<ApiResult<bool>> CancelAcceptedBookingAsync(Guid bookingId, string reason);
}
