// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/QueueService.cs
//  Student booking queue — accept, cancel, tab collections.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;
public class QueueService : IQueueService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public QueueService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    // ── Queue (Pending bookings scoped to student's assigned cubicle) ─
    public async Task<ApiResult<List<Booking>>> GetQueueAsync()
    {
        try
        {
            var r = await _supabase.From<Booking>()
                .Where(b => b.Status == BookingStatuses.Accepted)
                .Order("date", Postgrest.Constants.Ordering.Ascending)
                .Get();

            await EnrichAsync(r.Models);
            return ApiResult<List<Booking>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Booking>>.Fail(ex.Message); }
    }
    // ── Accepted by this student ──────────────────────────────────────
    public async Task<ApiResult<List<Booking>>> GetAcceptedAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Booking>>.Ok(new());

            var assignments = (await _supabase.From<BookingAssignment>()
                .Where(a => a.StudentId == uid).Get()).Models;

            var bookingIds = assignments.Select(a => a.BookingId).ToHashSet();
            if (!bookingIds.Any()) return ApiResult<List<Booking>>.Ok(new());

            var r = await _supabase.From<Booking>()
                .Where(b => b.Status == BookingStatuses.StudentAccepted)
                .Get();

            var filtered = r.Models
                .Where(b => bookingIds.Contains(b.Id))
                .OrderBy(b => b.Date)
                .ToList();

            await EnrichAsync(filtered);
            return ApiResult<List<Booking>>.Ok(filtered);
        }
        catch (Exception ex) { return ApiResult<List<Booking>>.Fail(ex.Message); }
    }
    public async Task<ApiResult<List<Booking>>> GetInProgressAsync()
        => await GetByStatusForStudentAsync("InProgress");

    public async Task<ApiResult<List<Booking>>> GetCompletedAsync()
        => await GetByStatusForStudentAsync("Completed");

    public async Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId)
    {
        try
        {
            var b = await _supabase.From<Booking>()
                .Where(x => x.Id == bookingId).Single();
            if (b is null)
                return ApiResult<Booking>.Fail("Booking not found.");
            await EnrichAsync(new List<Booking> { b });
            return ApiResult<Booking>.Ok(b);
        }
        catch (Exception ex) { return ApiResult<Booking>.Fail(ex.Message); }
    }

    // ── Accept ────────────────────────────────────────────────────────
    public async Task<ApiResult<Booking>> AcceptBookingAsync(Guid bookingId)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<Booking>.Fail("Not signed in.");

            await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId)
                .Set(b => b.Status, BookingStatuses.StudentAccepted)
                .Update();

            var existing = await _supabase.From<BookingAssignment>()
                .Where(a => a.BookingId == bookingId).Single();

            if (existing is null)
            {
                await _supabase.From<BookingAssignment>().Insert(new BookingAssignment
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingId,
                    StudentId = uid,
                    CubicleId = null,
                    SupervisorId = null,
                });
            }
            else
            {
                await _supabase.From<BookingAssignment>()
                    .Where(a => a.BookingId == bookingId)
                    .Set(a => a.StudentId, uid)
                    .Update();
            }

            var updated = await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId).Single();
            await EnrichAsync(new List<Booking> { updated! });
            return ApiResult<Booking>.Ok(updated!);
        }
        catch (Exception ex) { return ApiResult<Booking>.Fail(ex.Message); }
    }

    // ── Cancel accepted booking ───────────────────────────────────────
    public async Task<ApiResult<bool>> CancelAcceptedBookingAsync(Guid bookingId, string reason)
    {
        try
        {
            await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId)
                .Set(b => b.Status, BookingStatuses.Accepted)
                .Update();

            await _supabase.From<BookingAssignment>()
                .Where(a => a.BookingId == bookingId)
                .Set(a => a.StudentId, (Guid?)null)
                .Update();

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private async Task<ApiResult<List<Booking>>> GetByStatusForStudentAsync(string status)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Booking>>.Ok(new());

            var assignments = await _supabase.From<BookingAssignment>()
                .Where(a => a.StudentId == uid).Get();
            var ids = assignments.Models.Select(a => a.BookingId).ToList();

            if (!ids.Any()) return ApiResult<List<Booking>>.Ok(new());

            var r = await _supabase.From<Booking>()
                .Where(b => b.Status == status).Get();

            var filtered = r.Models.Where(b => ids.Contains(b.Id))
                .OrderByDescending(b => b.Date).ToList();

            await EnrichAsync(filtered);
            return ApiResult<List<Booking>>.Ok(filtered);
        }
        catch (Exception ex) { return ApiResult<List<Booking>>.Fail(ex.Message); }
    }

    private async Task EnrichAsync(List<Booking> bookings)
    {
        if (!bookings.Any()) return;
        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;

        foreach (var b in bookings)
        {
            b.ClinicName     = clinics.FirstOrDefault(c => c.Id == b.ClinicId)?.Name    ?? string.Empty;
            b.SessionDisplay = sessions.FirstOrDefault(s => s.Id == b.SessionId)?.Display ?? string.Empty;
        }
    }
}
