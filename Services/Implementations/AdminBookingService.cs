// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/AdminBookingService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class AdminBookingService : IAdminBookingService
{
    private readonly Supabase.Client _supabase;
    private readonly INotificationService _notifications;

    public AdminBookingService(Supabase.Client supabase, INotificationService notifications)
    {
        _supabase = supabase;
        _notifications = notifications;
    }

    // ── Get all bookings ──────────────────────────────────────────────
    public async Task<ApiResult<List<Booking>>> GetAllBookingsAsync(
        string? status = null)
    {
        try
        {
            var r = status is null
                ? await _supabase.From<Booking>()
                    .Order("date", Postgrest.Constants.Ordering.Descending).Get()
                : await _supabase.From<Booking>()
                    .Where(b => b.Status == status)
                    .Order("date", Postgrest.Constants.Ordering.Descending).Get();

            await EnrichAsync(r.Models);
            return ApiResult<List<Booking>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Booking>>.Fail(ex.Message); }
    }

    // ── Get single booking ────────────────────────────────────────────
    public async Task<ApiResult<Booking>> GetBookingByIdAsync(Guid bookingId)
    {
        try
        {
            var b = await _supabase.From<Booking>()
                .Where(x => x.Id == bookingId).Single();
            if (b is null) return ApiResult<Booking>.Fail("Booking not found.");
            await EnrichAsync(new List<Booking> { b });
            return ApiResult<Booking>.Ok(b);
        }
        catch (Exception ex) { return ApiResult<Booking>.Fail(ex.Message); }
    }

    // ── Assign supervisor + cubicle to a booking ──────────────────────
    public async Task<ApiResult<Booking>> AssignBookingAsync(
        AssignBookingRequest request)
    {
        try
        {
            // Upsert assignment row
            var existing = await _supabase.From<BookingAssignment>()
                .Where(a => a.BookingId == request.BookingId).Single();

            if (existing is null)
            {
                await _supabase.From<BookingAssignment>().Insert(new BookingAssignment
                {
                    Id = Guid.NewGuid(),
                    BookingId = request.BookingId,
                    SupervisorId = request.SupervisorId,
                    StudentId = request.StudentId == Guid.Empty
                                       ? null : request.StudentId,
                    CubicleId = request.CubicleId == 0
                                       ? null : request.CubicleId,
                });
            }
            else
            {
                await _supabase.From<BookingAssignment>()
                    .Where(a => a.BookingId == request.BookingId)
                    .Set(a => a.SupervisorId, request.SupervisorId)
                    .Set(a => a.CubicleId,    request.CubicleId == 0 ? (int?)null : request.CubicleId)
                    .Update();
            }

            // Move booking status to Accepted
            await _supabase.From<Booking>()
                .Where(b => b.Id == request.BookingId)
                .Set(b => b.Status, BookingStatuses.InProgress)
                .Update();

            var updated = await _supabase.From<Booking>()
                .Where(b => b.Id == request.BookingId).Single();
            if (updated is null) return ApiResult<Booking>.Fail("Booking not found after assignment.");

            await _notifications.SendToUserAsync(
                request.SupervisorId,
                "Booking Assigned",
                "A booking has been assigned to you and is ready for encounter review.",
                "booking_assignment");

            return ApiResult<Booking>.Ok(updated);
        }
        catch (Exception ex) { return ApiResult<Booking>.Fail(ex.Message); }
    }

    // ── Auto-assign all pending bookings round-robin ──────────────────
    public async Task<ApiResult<int>> AutoAssignAllPendingAsync()
    {
        try
        {
            var pending = await _supabase.From<Booking>()
                .Where(b => b.Status == BookingStatuses.Pending).Get();

            if (!pending.Models.Any()) return ApiResult<int>.Ok(0);

            var supervisors = await _supabase.From<SupervisorProfile>().Get();
            var supCubicles = await _supabase.From<SupervisorCubicle>().Get();

            var supMap = supervisors.Models
                .Select(s =>
                {
                    s.AssignedCubicleIds = supCubicles.Models
                        .Where(sc => sc.SupervisorId == s.Id)
                        .Select(sc => sc.CubicleId)
                        .ToList();
                    return s;
                })
                .Where(s => s.AssignedCubicleIds.Any())
                .ToList();

            if (!supMap.Any()) return ApiResult<int>.Ok(0);

            int assigned = 0;
            int supIndex = 0;

            foreach (var booking in pending.Models)
            {
                var supervisor = supMap[supIndex % supMap.Count];
                var cubicleId = supervisor.AssignedCubicleIds.First();

                await AssignBookingAsync(new AssignBookingRequest
                {
                    BookingId = booking.Id,
                    SupervisorId = supervisor.UserId,
                    StudentId = Guid.Empty,
                    CubicleId = cubicleId,
                });

                supIndex++;
                assigned++;
            }

            return ApiResult<int>.Ok(assigned);
        }
        catch (Exception ex) { return ApiResult<int>.Fail(ex.Message); }
    }

    // ── Cancel a booking ──────────────────────────────────────────────
    public async Task<ApiResult<bool>> CancelBookingAsync(
        Guid bookingId, string reason)
    {
        try
        {
            await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId)
                .Set(b => b.Status, BookingStatuses.Cancelled)
                .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Admin books on behalf of a patient ────────────────────────────
    public async Task<ApiResult<Booking>> BookForPatientAsync(
        AdminBookForPatientRequest request)
    {
        try
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                PatientId = request.PatientId,
                ClinicId = request.ClinicId,
                SessionId = request.SessionId,
                Date = request.Date.Date,
                Status = BookingStatuses.Pending,
                BookingType = request.BookingType,
                BookedByAdmin = request.BookedByAdmin,
            };

            var r = await _supabase.From<Booking>().Insert(booking);
            var saved = r.Models.First();

            // Enrich display names
            await EnrichAsync(new List<Booking> { saved });

            return ApiResult<Booking>.Ok(saved);
        }
        catch (Exception ex)
        {
            return ApiResult<Booking>.Fail(ex.Message);
        }
    }

    // ── Enrich bookings with joined display fields ────────────────────
    private async Task EnrichAsync(List<Booking> bookings)
    {
        if (!bookings.Any()) return;

        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;
        var profiles = (await _supabase.From<UserProfile>().Get()).Models;
        var assigns  = (await _supabase.From<BookingAssignment>().Get()).Models;
        var cubicles = (await _supabase.From<Cubicle>().Get()).Models;

        foreach (var b in bookings)
        {
            b.ClinicName     = clinics.FirstOrDefault(c => c.Id == b.ClinicId)?.Name     ?? string.Empty;
            b.SessionDisplay = sessions.FirstOrDefault(s => s.Id == b.SessionId)?.Display ?? string.Empty;

            var patient = profiles.FirstOrDefault(p => p.UserId == b.PatientId);
            if (patient is not null)
                b.PatientName = patient.FullName;

            var assign = assigns.FirstOrDefault(a => a.BookingId == b.Id);
            if (assign is null) continue;

            if (assign.SupervisorId.HasValue)
                b.SupervisorName = profiles.FirstOrDefault(p =>
                    p.UserId == assign.SupervisorId.Value)?.FullName ?? string.Empty;

            if (assign.StudentId.HasValue)
                b.StudentName = profiles.FirstOrDefault(p =>
                    p.UserId == assign.StudentId.Value)?.FullName ?? string.Empty;

            if (assign.CubicleId.HasValue)
                b.CubicleNumber = cubicles.FirstOrDefault(c =>
                    c.Id == assign.CubicleId.Value)?.Name ?? string.Empty;
        }
    }
}
