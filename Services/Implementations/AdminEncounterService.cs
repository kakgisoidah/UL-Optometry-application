// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/AdminEncounterService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class AdminEncounterService : IAdminEncounterService
{
    private readonly Supabase.Client _supabase;

    public AdminEncounterService(Supabase.Client supabase)
        => _supabase = supabase;

    public async Task<ApiResult<List<Encounter>>> GetAllEncountersAsync(
        string? status = null)
    {
        try
        {
            var r = status is null
                ? await _supabase.From<Encounter>()
                    .Order("created_at", Postgrest.Constants.Ordering.Descending).Get()
                : await _supabase.From<Encounter>()
                    .Where(e => e.Status == status)
                    .Order("created_at", Postgrest.Constants.Ordering.Descending).Get();

            await EnrichAsync(r.Models);
            return ApiResult<List<Encounter>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<Encounter>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<Encounter>> GetEncounterByIdAsync(Guid encounterId)
    {
        try
        {
            var e = await _supabase.From<Encounter>()
                .Where(x => x.Id == encounterId).Single();
            if (e is null) return ApiResult<Encounter>.Fail("Not found.");

            var attachments = await _supabase.From<EncounterAttachment>()
                .Where(a => a.EncounterId == encounterId).Get();
            e.Attachments = attachments.Models;

            await EnrichAsync(new List<Encounter> { e });
            return ApiResult<Encounter>.Ok(e);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    public async Task<ApiResult<string>> GetPdfUrlAsync(Guid encounterId)
    {
        try
        {
            var response = await _supabase.Functions.Invoke<PdfUrlResponse>(
                "generate-encounter-pdf",
                _supabase.Auth.CurrentSession?.AccessToken,
                new Supabase.Functions.Client.InvokeFunctionOptions
                {
                    Body = new Dictionary<string, object> { ["encounterId"] = encounterId.ToString() }
                });

            return response?.Url is not null
                ? ApiResult<string>.Ok(response.Url)
                : ApiResult<string>.Fail("PDF generation failed.");
        }
        catch
        {
            return ApiResult<string>.Fail(
                "PDF service not available. Deploy the generate-encounter-pdf Edge Function.");
        }
    }

    private async Task EnrichAsync(List<Encounter> encounters)
    {
        if (!encounters.Any()) return;
        var profiles = (await _supabase.From<UserProfile>().Get()).Models;
        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;

        // Pre-fetch all bookings in one round-trip instead of one per encounter (N+1 fix)
        var bookingIds = encounters
            .Where(e => e.BookingId.HasValue)
            .Select(e => e.BookingId!.Value)
            .ToHashSet();
        var bookings = bookingIds.Any()
            ? (await _supabase.From<Booking>().Get()).Models
                .Where(b => bookingIds.Contains(b.Id)).ToList()
            : new List<Booking>();

        foreach (var e in encounters)
        {
            var stuProf = profiles.FirstOrDefault(p => p.UserId == e.StudentId);
            e.StudentName     = stuProf?.FullName ?? string.Empty;
            e.StudentInitials = stuProf?.Initials ?? "?";

            if (!e.BookingId.HasValue) continue;
            var booking = bookings.FirstOrDefault(b => b.Id == e.BookingId.Value);
            if (booking is null) continue;

            e.ClinicName  = clinics.FirstOrDefault(c => c.Id == booking.ClinicId)?.Name     ?? string.Empty;
            e.SlotDisplay = sessions.FirstOrDefault(s => s.Id == booking.SessionId)?.Display ?? string.Empty;
        }
    }
}

file record PdfUrlResponse(string? Url);
