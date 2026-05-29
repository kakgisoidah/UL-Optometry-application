// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/SchedulingService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Admin;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class SchedulingService : ISchedulingService
{
    private readonly Supabase.Client _supabase;

    public SchedulingService(Supabase.Client supabase)
        => _supabase = supabase;

    // ── Sessions ──────────────────────────────────────────────────────
    public async Task<ApiResult<List<Session>>> GetSessionsAsync()
    {
        try
        {
            var r = await _supabase.From<Session>()
                .Order("id", Postgrest.Constants.Ordering.Ascending).Get();
            return ApiResult<List<Session>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Session>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<Session>> AddSessionAsync(
        string name, string startTime, string endTime)
    {
        try
        {
            var count = (await _supabase.From<Session>().Get()).Models.Count;
            if (count >= 3)
                return ApiResult<Session>.Fail("Maximum of 3 sessions reached.");

            var r = await _supabase.From<Session>().Insert(new Session
            {
                Name      = name,
                StartTime = startTime,
                EndTime   = endTime,
            });
            return ApiResult<Session>.Ok(r.Models.First());
        }
        catch (Exception ex) { return ApiResult<Session>.Fail(ex.Message); }
    }

    public async Task<ApiResult<Session>> UpdateSessionAsync(
        int sessionId, string name, string startTime, string endTime)
    {
        try
        {
            await _supabase.From<Session>()
                .Where(s => s.Id == sessionId)
                .Set(s => s.Name,      name)
                .Set(s => s.StartTime, startTime)
                .Set(s => s.EndTime,   endTime)
                .Update();

            var r = await _supabase.From<Session>()
                .Where(s => s.Id == sessionId).Single();
            return ApiResult<Session>.Ok(r!);
        }
        catch (Exception ex) { return ApiResult<Session>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> DeleteSessionAsync(int sessionId)
    {
        try
        {
            await _supabase.From<Session>()
                .Where(s => s.Id == sessionId).Delete();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Cubicles ──────────────────────────────────────────────────────
    public async Task<ApiResult<List<Cubicle>>> GetCubiclesAsync()
    {
        try
        {
            var r = await _supabase.From<Cubicle>()
                .Where(c => c.IsActive == true)
                .Order("id", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return ApiResult<List<Cubicle>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Cubicle>>.Fail(ex.Message); }
    }

    //remove
    

    public async Task<ApiResult<Cubicle>> AddCubicleAsync(string name)
    {
        try
        {
            var r = await _supabase.From<Cubicle>()
                .Insert(new Cubicle { Name = name });
            return ApiResult<Cubicle>.Ok(r.Models.First());
        }
        catch (Exception ex) { return ApiResult<Cubicle>.Fail(ex.Message); }
    }

    // ── Assignments ───────────────────────────────────────────────────
    public async Task<ApiResult<List<CubicleAssignment>>> GetAssignmentsAsync(
        int sessionId)
    {
        try
        {
            var cubicles    = (await _supabase.From<Cubicle>().Where(c => c.IsActive == true).Get()).Models;
            var profiles    = (await _supabase.From<UserProfile>().Get()).Models;
            var supervisors = (await _supabase.From<Models.Admin.SupervisorProfile>().Get()).Models;
            var supCubicles = (await _supabase.From<Models.Admin.SupervisorCubicle>().Get()).Models;

            // Populate AssignedCubicleIds by joining supervisor_cubicles before using it
            foreach (var s in supervisors)
                s.AssignedCubicleIds = supCubicles
                    .Where(sc => sc.SupervisorId == s.Id)
                    .Select(sc => sc.CubicleId).ToList();

            // Scope student assignments to the requested session's bookings
            var sessionBookings = sessionId > 0
                ? (await _supabase.From<Booking>()
                    .Where(b => b.SessionId == sessionId).Get()).Models
                : new List<Booking>();
            var sessionBookingIds = sessionBookings.Select(b => b.Id).ToHashSet();

            var assignments = (await _supabase.From<BookingAssignment>().Get()).Models;

            var result = cubicles.Select(cub =>
            {
                // Prefer session-scoped assignment; fall back to any assignment for this cubicle
                var assign = assignments.FirstOrDefault(a =>
                    a.CubicleId == cub.Id && sessionBookingIds.Contains(a.BookingId))
                    ?? assignments.FirstOrDefault(a => a.CubicleId == cub.Id);

                var stuName = assign?.StudentId.HasValue == true
                    ? profiles.FirstOrDefault(p => p.UserId == assign.StudentId.Value)?.FullName ?? string.Empty
                    : string.Empty;
                var supProfile = supervisors.FirstOrDefault(s =>
                    s.AssignedCubicleIds.Contains(cub.Id));
                var supName = supProfile is not null
                    ? profiles.FirstOrDefault(p => p.UserId == supProfile.UserId)?.FullName ?? string.Empty
                    : string.Empty;

                return new CubicleAssignment
                {
                    CubicleId      = cub.Id,
                    CubicleName    = cub.Name,
                    StudentId      = assign?.StudentId,
                    StudentName    = stuName,
                    SupervisorId   = supProfile is not null ? supProfile.UserId : null,
                    SupervisorName = supName,
                };
            }).ToList();

            return ApiResult<List<CubicleAssignment>>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResult<List<CubicleAssignment>>.Fail(ex.Message);
        }
    }

    //supervisor assignment
    public async Task<ApiResult<bool>> AssignSupervisorAsync(int cubicleId, Guid supervisorUserId, DateTime date)
    {
        try
        {
            var assignedDate = date.Date;

            // Remove existing assignment for this cubicle today if any
            await _supabase.From<SupervisorDailyAssignment>()
                .Where(a => a.CubicleId == cubicleId && a.AssignedDate == assignedDate)
                .Delete();

            // Insert today's assignment
            await _supabase.From<SupervisorDailyAssignment>().Insert(new SupervisorDailyAssignment
            {
                Id = Guid.NewGuid(),
                SupervisorId = supervisorUserId,
                CubicleId = cubicleId,
                AssignedDate = assignedDate,
            });

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // Get supervisor assignments for a specific date
    public async Task<ApiResult<List<SupervisorDailyAssignment>>> GetDailyAssignmentsAsync(DateTime date)
    {
        try
        {
            var r = await _supabase.From<SupervisorDailyAssignment>()
                .Where(a => a.AssignedDate == date.Date)
                .Get();
            return ApiResult<List<SupervisorDailyAssignment>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<SupervisorDailyAssignment>>.Fail(ex.Message); }
    }

    //remove supervisor assignment
    public async Task RemoveSupervisorAsync(int cubicleId, DateTime date)
    {
        await _supabase.From<SupervisorDailyAssignment>()
            .Where(a => a.CubicleId == cubicleId && a.AssignedDate == date.Date)
            .Delete();
    }

    public async Task<ApiResult<bool>> AssignStudentAsync(
        int cubicleId, int sessionId, Guid studentUserId)
    {
        try
        {
            // Enforce 1 cubicle per student — scoped to the current session's bookings only
            var sessionBookings = (await _supabase.From<Booking>()
                .Where(b => b.SessionId == sessionId).Get()).Models;
            var sessionBookingIds = sessionBookings.Select(b => b.Id).ToHashSet();

            var allAssignments = (await _supabase.From<BookingAssignment>().Get()).Models;
            var conflict = allAssignments.FirstOrDefault(a =>
                sessionBookingIds.Contains(a.BookingId) &&
                a.StudentId == studentUserId &&
                a.CubicleId.HasValue &&
                a.CubicleId.Value != cubicleId);

            if (conflict is not null)
            {
                var cubicles = (await _supabase.From<Cubicle>().Get()).Models;
                var conflictName = cubicles.FirstOrDefault(c => c.Id == conflict.CubicleId)?.Name ?? "another cubicle";
                return ApiResult<bool>.Fail(
                    $"Student is already assigned to {conflictName} for this session. Each student may only hold one cubicle per session.");
            }

            // Update only assignments that belong to the current session AND this cubicle
            // (allAssignments was already fetched above — reuse it, no extra round-trip)
            var sessionAssignments = allAssignments
                .Where(a => sessionBookingIds.Contains(a.BookingId) && a.CubicleId == cubicleId)
                .ToList();

            foreach (var a in sessionAssignments)
            {
                await _supabase.From<BookingAssignment>()
                    .Where(x => x.Id == a.Id)
                    .Set(x => x.StudentId, (Guid?)studentUserId)
                    .Update();
            }
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> RemoveStudentAsync(int cubicleId, int sessionId)
    {
        try
        {
            // Scope removal to the current session's bookings only (sessionId was previously ignored)
            var sessionBookings = (await _supabase.From<Booking>()
                .Where(b => b.SessionId == sessionId).Get()).Models;
            var sessionBookingIds = sessionBookings.Select(b => b.Id).ToHashSet();

            var assignments = (await _supabase.From<BookingAssignment>()
                .Where(a => a.CubicleId == cubicleId).Get()).Models
                .Where(a => sessionBookingIds.Contains(a.BookingId))
                .ToList();

            foreach (var a in assignments)
            {
                await _supabase.From<BookingAssignment>()
                    .Where(x => x.Id == a.Id)
                    .Set(x => x.StudentId, (Guid?)null)
                    .Update();
            }
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Get available slots ───────────────────────────────────────────
    public async Task<ApiResult<List<BlockedDate>>> GetBlockedDatesAsync()
    {
        try
        {
            var r = await _supabase.From<BlockedDate>()
                .Order("date", Postgrest.Constants.Ordering.Ascending).Get();
            return ApiResult<List<BlockedDate>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<BlockedDate>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<BlockedDate>> BlockDateAsync(DateTime date, string reason)
    {
        try
        {
            var entry = new BlockedDate { Date = date.Date, Reason = reason };
            var r = await _supabase.From<BlockedDate>().Insert(entry);
            return ApiResult<BlockedDate>.Ok(r.Models.First());
        }
        catch (Exception ex) { return ApiResult<BlockedDate>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> UnblockDateAsync(int blockedDateId)
    {
        try
        {
            await _supabase.From<BlockedDate>()
                .Where(b => b.Id == blockedDateId).Delete();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }
}
