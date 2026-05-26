// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/PatientEncounterService.cs
//  Read-only — only Approved encounters, only for this patient's bookings.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;

using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class PatientEncounterService : IPatientEncounterService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public PatientEncounterService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    public async Task<ApiResult<List<Encounter>>> GetMyEncountersAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Encounter>>.Ok(new());

            // Get patient's booking IDs
            var bookings = await _supabase.From<Booking>()
                .Where(b => b.PatientId == uid)
                .Get();
            var bookingIds = bookings.Models.Select(b => b.Id).ToList();

            if (!bookingIds.Any())
                return ApiResult<List<Encounter>>.Ok(new());

            // Get approved encounters for those bookings
            var r = await _supabase.From<Encounter>()
                .Where(e => e.Status == "Approved")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            // Filter to patient's bookings (RLS also enforces this)
            var filtered = r.Models
                .Where(e => e.BookingId.HasValue && bookingIds.Contains(e.BookingId.Value))
                .ToList();

            await EnrichAsync(filtered);
            return ApiResult<List<Encounter>>.Ok(filtered);
        }
        catch (Exception ex)
        {
            return ApiResult<List<Encounter>>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<Encounter>> GetEncounterByIdAsync(Guid encounterId)
    {
        try
        {
            var e = await _supabase.From<Encounter>()
                .Where(x => x.Id == encounterId && x.Status == "Approved")
                .Single();

            if (e is null)
                return ApiResult<Encounter>.Fail("Encounter not found.");

            await EnrichAsync(new List<Encounter> { e });
            return ApiResult<Encounter>.Ok(e);
        }
        catch (Exception ex)
        {
            return ApiResult<Encounter>.Fail(ex.Message);
        }
    }

    private async Task EnrichAsync(List<Encounter> encounters)
    {
        if (!encounters.Any()) return;

        // Pre-fetch all relevant bookings, clinics, sessions in one pass (N+1 fix)
        var bookingIds = encounters
            .Where(e => e.BookingId.HasValue)
            .Select(e => e.BookingId!.Value)
            .ToHashSet();

        var bookings = bookingIds.Any()
            ? (await _supabase.From<Booking>().Get()).Models
                .Where(b => bookingIds.Contains(b.Id)).ToList()
            : new List<Booking>();

        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;

        foreach (var e in encounters)
        {
            if (!e.BookingId.HasValue) continue;
            var booking = bookings.FirstOrDefault(b => b.Id == e.BookingId.Value);
            if (booking is null) continue;

            e.ClinicName  = clinics.FirstOrDefault(c => c.Id == booking.ClinicId)?.Name     ?? string.Empty;
            e.SlotDisplay = sessions.FirstOrDefault(s => s.Id == booking.SessionId)?.Display ?? string.Empty;
        }
    }
}
