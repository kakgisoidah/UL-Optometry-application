// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/BookingService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public BookingService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    // ── Clinics ───────────────────────────────────────────────────────
    public async Task<ApiResult<List<Clinic>>> GetClinicsAsync()
    {
        try
        {
            var r = await _supabase.From<Clinic>()
                .Order("weekday", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return ApiResult<List<Clinic>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Clinic>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<Clinic>> GetClinicByWeekdayAsync(int weekday)
    {
        try
        {
            var r = await _supabase.From<Clinic>()
                .Where(c => c.Weekday == weekday)
                .Single();
            return r is not null
                ? ApiResult<Clinic>.Ok(r)
                : ApiResult<Clinic>.Fail("Clinic not found.");
        }
        catch (Exception ex) { return ApiResult<Clinic>.Fail(ex.Message); }
    }

    // ── Available slots ───────────────────────────────────────────────
    public async Task<ApiResult<List<Session>>> GetAvailableSlotsAsync(
        DateTime date, int clinicId)
    {
        try
        {
            // Get all sessions
            var sessions = await _supabase.From<Session>()
                .Order("id", Postgrest.Constants.Ordering.Ascending)
                .Get();

            // Count bookings per session for this date (not cancelled)
            var bookings = await _supabase.From<Booking>()
                .Filter("date", Postgrest.Constants.Operator.Equals, date.Date.ToString("yyyy-MM-dd"))
                .Filter("clinic_id", Postgrest.Constants.Operator.Equals, clinicId.ToString())
                .Filter("status", Postgrest.Constants.Operator.NotEqual, "Cancelled")
                .Get();

            var bookedSessionIds = bookings.Models
                .GroupBy(b => b.SessionId)
                .Where(g => g.Count() >= 10)   // max 10 patients per slot
                .Select(g => g.Key)
                .ToHashSet();

            var available = sessions.Models
                .Where(s => !bookedSessionIds.Contains(s.Id))
                .ToList();

            return ApiResult<List<Session>>.Ok(available);
        }
        catch (Exception ex)
        {
            return ApiResult<List<Session>>.Fail(ex.Message);
        }
    }

    // ── My bookings (patient) ─────────────────────────────────────────
    public async Task<ApiResult<List<Booking>>> GetMyBookingsAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Booking>>.Ok(new());

            var r = await _supabase.From<Booking>()
                .Where(b => b.PatientId == uid)
                .Order("date", Postgrest.Constants.Ordering.Descending)
                .Get();

            // Enrich with clinic + session display names
            var clinics  = (await _supabase.From<Clinic>().Get()).Models;
            var sessions = (await _supabase.From<Session>().Get()).Models;

            foreach (var b in r.Models)
            {
                b.ClinicName     = clinics.FirstOrDefault(c => c.Id == b.ClinicId)?.Name ?? string.Empty;
                b.SessionDisplay = sessions.FirstOrDefault(s => s.Id == b.SessionId)?.Display ?? string.Empty;
            }

            return ApiResult<List<Booking>>.Ok(r.Models);
        }
        catch (Exception ex)
        {
            return ApiResult<List<Booking>>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId)
    {
        try
        {
            var b = await _supabase.From<Booking>()
                .Where(x => x.Id == bookingId)
                .Single();

            if (b is null)
                return ApiResult<Booking>.Fail("Booking not found.");

            // Pre-fetch all lookup tables once instead of individual single-row fetches
            var clinics  = (await _supabase.From<Clinic>().Get()).Models;
            var sessions = (await _supabase.From<Session>().Get()).Models;
            var cubicles = (await _supabase.From<Cubicle>().Get()).Models;
            var profiles = (await _supabase.From<UserProfile>().Get()).Models;
            var assign   = await _supabase.From<BookingAssignment>()
                .Where(a => a.BookingId == bookingId).Single();

            b.ClinicName     = clinics.FirstOrDefault(c => c.Id == b.ClinicId)?.Name    ?? string.Empty;
            b.SessionDisplay = sessions.FirstOrDefault(s => s.Id == b.SessionId)?.Display ?? string.Empty;

            if (assign is not null)
            {
                if (assign.CubicleId.HasValue)
                    b.CubicleNumber = cubicles.FirstOrDefault(c => c.Id == assign.CubicleId.Value)?.Name
                                      ?? string.Empty;

                if (assign.SupervisorId.HasValue)
                    b.SupervisorName = profiles.FirstOrDefault(p => p.UserId == assign.SupervisorId.Value)?.FullName
                                       ?? string.Empty;

                if (assign.StudentId.HasValue)
                    b.StudentName = profiles.FirstOrDefault(p => p.UserId == assign.StudentId.Value)?.FullName
                                    ?? string.Empty;
            }

            return ApiResult<Booking>.Ok(b);
        }
        catch (Exception ex)
        {
            return ApiResult<Booking>.Fail(ex.Message);
        }
    }

    // ── Create ────────────────────────────────────────────────────────
    public async Task<ApiResult<Booking>> CreateBookingAsync(CreateBookingRequest request)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<Booking>.Fail("Not signed in.");

            var booking = new Booking
            {
                Id        = Guid.NewGuid(),
                PatientId = uid,
                ClinicId  = request.ClinicId,
                SessionId = request.SessionId,
                Date      = request.Date.Date,
                Status = BookingStatuses.Accepted,
            };

            var r = await _supabase.From<Booking>().Insert(booking);
            return ApiResult<Booking>.Ok(r.Models.First());
        }
        catch (Exception ex)
        {
            return ApiResult<Booking>.Fail(ex.Message);
        }
    }

    // ── Cancel ────────────────────────────────────────────────────────
    public async Task<ApiResult<bool>> CancelBookingAsync(CancelBookingRequest request)
    {
        try
        {
            await _supabase.From<Booking>()
                .Where(b => b.Id == request.BookingId)
                .Set(b => b.Status, "Cancelled")
                .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail(ex.Message);
        }
    }
}
