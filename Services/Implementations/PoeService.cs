// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/PoeService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class PoeService : IPoeService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public PoeService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    // ── Student ───────────────────────────────────────────────────────
    public async Task<ApiResult<PoeSummary>> GetPoeSummaryAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<PoeSummary>.Ok(new PoeSummary());

            return await BuildSummaryAsync(uid);
        }
        catch (Exception ex) { return ApiResult<PoeSummary>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<RecentPoeEncounter>>> GetRecentEntriesAsync(int limit = 10)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<RecentPoeEncounter>>.Ok(new());

            var entries = await _supabase.From<PoeEntry>()
                .Where(e => e.StudentId == uid)
                .Order("credited_at", Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            var cats      = (await _supabase.From<PoeCategory>().Get()).Models;
            var clinics   = (await _supabase.From<Clinic>().Get()).Models;
            var encounters = (await _supabase.From<Encounter>()
                .Where(e => e.StudentId == uid).Get()).Models;
            var bookingIds = encounters
                .Where(e => e.BookingId.HasValue)
                .Select(e => e.BookingId!.Value)
                .ToHashSet();
            var bookings  = bookingIds.Any()
                ? (await _supabase.From<Booking>().Get()).Models
                    .Where(b => bookingIds.Contains(b.Id)).ToList()
                : new List<Booking>();

            var recent = entries.Models.Select(e =>
            {
                var encounter   = encounters.FirstOrDefault(enc => enc.Id == e.EncounterId);
                var clinicName  = string.Empty;
                if (encounter?.BookingId.HasValue == true)
                {
                    var booking = bookings.FirstOrDefault(b => b.Id == encounter.BookingId.Value);
                    clinicName  = clinics.FirstOrDefault(c => c.Id == booking?.ClinicId)?.Name ?? string.Empty;
                }
                return new RecentPoeEncounter
                {
                    CategoryName = cats.FirstOrDefault(c => c.Id == e.CategoryId)?.Name ?? string.Empty,
                    Date         = e.CreditedAt,
                    Hours        = e.Hours,
                    ClinicName   = clinicName,
                };
            }).ToList();

            return ApiResult<List<RecentPoeEncounter>>.Ok(recent);
        }
        catch (Exception ex)
        {
            return ApiResult<List<RecentPoeEncounter>>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<List<PoeCategory>>> GetCategoriesAsync()
    {
        try
        {
            var r = await _supabase.From<PoeCategory>()
                .Order("id", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return ApiResult<List<PoeCategory>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<PoeCategory>>.Fail(ex.Message); }
    }

    // ── Admin ─────────────────────────────────────────────────────────
    public async Task<ApiResult<PoeSummary>> GetPoeSummaryForStudentAsync(Guid studentUserId)
    {
        try { return await BuildSummaryAsync(studentUserId); }
        catch (Exception ex) { return ApiResult<PoeSummary>.Fail(ex.Message); }
    }

    public async Task<ApiResult<double>> GetAveragePoePercentAsync()
    {
        try
        {
            var students = await _supabase.From<Models.Admin.StudentProfile>().Get();
            if (!students.Models.Any()) return ApiResult<double>.Ok(0);

            // Pre-fetch all PoE entries once — no N+1 per student (N+1 fix)
            var allEntries = (await _supabase.From<PoeEntry>().Get()).Models;

            double total = 0;
            foreach (var s in students.Models)
            {
                var logged  = allEntries.Where(e => e.StudentId == s.UserId).Sum(e => e.Hours);
                total      += Math.Min(logged / PoeSummary.TotalRequired * 100, 100);
            }
            return ApiResult<double>.Ok(Math.Round(total / students.Models.Count, 1));
        }
        catch (Exception ex) { return ApiResult<double>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<(Guid UserId, string FullName, double PoePercent)>>>
        GetAllStudentPoeAsync()
    {
        try
        {
            var students   = await _supabase.From<Models.Admin.StudentProfile>().Get();
            var profiles   = (await _supabase.From<UserProfile>().Get()).Models;

            // Pre-fetch all PoE entries once — no N+1 per student (N+1 fix)
            var allEntries = (await _supabase.From<PoeEntry>().Get()).Models;

            var result = new List<(Guid, string, double)>();
            foreach (var s in students.Models)
            {
                var fullName = profiles.FirstOrDefault(p => p.UserId == s.UserId)?.FullName
                               ?? string.Empty;
                var logged   = allEntries.Where(e => e.StudentId == s.UserId).Sum(e => e.Hours);
                var percent  = Math.Round(Math.Min(logged / PoeSummary.TotalRequired * 100, 100), 1);
                result.Add((s.UserId, fullName, percent));
            }

            return ApiResult<List<(Guid, string, double)>>.Ok(
                result.OrderBy(x => x.Item3).ToList());
        }
        catch (Exception ex)
        {
            return ApiResult<List<(Guid, string, double)>>.Fail(ex.Message);
        }
    }

    // ── Private helper ────────────────────────────────────────────────
    private async Task<ApiResult<PoeSummary>> BuildSummaryAsync(Guid studentUserId)
    {
        var cats    = (await _supabase.From<PoeCategory>().Get()).Models;
        var entries = await _supabase.From<PoeEntry>()
            .Where(e => e.StudentId == studentUserId)
            .Get();

        var loggedByCat = entries.Models
            .GroupBy(e => e.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Hours));

        foreach (var cat in cats)
            cat.LoggedHours = loggedByCat.TryGetValue(cat.Id, out var h) ? h : 0;

        var summary = new PoeSummary
        {
            TotalLogged = cats.Sum(c => c.LoggedHours),
            Categories  = cats,
        };

        return ApiResult<PoeSummary>.Ok(summary);
    }
}
