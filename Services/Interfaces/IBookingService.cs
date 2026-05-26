// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IBookingService.cs
//  Patient booking flow — clinic selection, calendar, slots, CRUD.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;

namespace UL_Optometry.Services.Interfaces;

public interface IBookingService
{
    // ── Clinics ───────────────────────────────────────────────────────
    /// <summary>All active clinics ordered by weekday (1=Mon … 5=Fri).</summary>
    Task<ApiResult<List<Clinic>>> GetClinicsAsync();

    /// <summary>Returns the single clinic for the given weekday number.</summary>
    Task<ApiResult<Clinic>> GetClinicByWeekdayAsync(int weekday);

    // ── Sessions (time slots) ─────────────────────────────────────────
    /// <summary>
    /// Returns available (not fully booked) sessions for a specific date
    /// and clinic. Used on SelectSlotPage.
    /// </summary>
    Task<ApiResult<List<Session>>> GetAvailableSlotsAsync(DateTime date, int clinicId);

    // ── My bookings ───────────────────────────────────────────────────
    /// <summary>All bookings for the current patient, newest first.</summary>
    Task<ApiResult<List<Booking>>> GetMyBookingsAsync();

    /// <summary>Single booking by ID (with joined clinic + session data).</summary>
    Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId);

    // ── Create / cancel ───────────────────────────────────────────────
    /// <summary>
    /// Create a new booking.
    /// Inserts into public.bookings with status = "Pending".
    /// </summary>
    Task<ApiResult<Booking>> CreateBookingAsync(CreateBookingRequest request);

    /// <summary>
    /// Cancel a booking.
    /// Sets status = "Cancelled".
    /// Only allowed when CanCancel = true (Pending or Accepted status).
    /// </summary>
    Task<ApiResult<bool>> CancelBookingAsync(CancelBookingRequest request);
}
