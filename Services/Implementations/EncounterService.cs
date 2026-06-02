// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/EncounterService.cs
//  Student encounter full lifecycle: draft → submit → resubmit.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Common;

using UL_Optometry.Services.Interfaces;
namespace UL_Optometry.Services.Implementations;

public class EncounterService : IEncounterService
{
    private readonly Supabase.Client      _supabase;
    private readonly IAuthService         _auth;
    private readonly INotificationService _notifications;

    public EncounterService(
        Supabase.Client supabase, IAuthService auth, INotificationService notifications)
    {
        _supabase      = supabase;
        _auth          = auth;
        _notifications = notifications;
    }

    // ── Lists ─────────────────────────────────────────────────────────
    public async Task<ApiResult<List<Encounter>>> GetMyEncountersAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Encounter>>.Ok(new());

            var r = await _supabase.From<Encounter>()
                .Where(e => e.StudentId == uid)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

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

            if (e is null)
                return ApiResult<Encounter>.Fail("Encounter not found.");

            // Load attachments
            var attachments = await _supabase.From<EncounterAttachment>()
                .Where(a => a.EncounterId == encounterId).Get();
            e.Attachments = attachments.Models;

            await EnrichAsync(new List<Encounter> { e });
            return ApiResult<Encounter>.Ok(e);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Booking prepopulation ─────────────────────────────────────────
    public async Task<ApiResult<Encounter>> GetPrepopulatedFromBookingAsync(Guid bookingId)
    {
        try
        {
            var booking = await _supabase.From<Booking>()
                .Where(b => b.Id == bookingId).Single();
            if (booking is null)
                return ApiResult<Encounter>.Fail("Booking not found.");

            var clinics   = (await _supabase.From<Clinic>().Get()).Models;
            var sessions  = (await _supabase.From<Session>().Get()).Models;
            var cubicles  = (await _supabase.From<Cubicle>().Get()).Models;
            var ba        = (await _supabase.From<BookingAssignment>()
                                .Where(a => a.BookingId == bookingId).Single());

            var patient = await _supabase.From<Models.Admin.PatientDbProfile>()
                .Where(p => p.UserId == booking.PatientId).Single();
            var patientProfile = await _supabase.From<UserProfile>()
                .Where(p => p.UserId == booking.PatientId).Single();

            var e = new Encounter
            {
                BookingId         = bookingId,
                EncounterType     = EncounterTypes.Onsite,
                PatientName       = patientProfile?.FullName ?? string.Empty,
                PatientFileNumber = patient?.IdNumber ?? string.Empty,
                Dob               = patient?.DateOfBirth?.ToString("dd MMM yyyy") ?? string.Empty,
                Gender            = patient?.Gender ?? string.Empty,
                SessionId         = booking.SessionId,
                CubicleId         = ba?.CubicleId,
                // Joined fields
                ClinicName  = clinics.FirstOrDefault(c => c.Id == booking.ClinicId)?.Name ?? string.Empty,
                SlotDisplay = sessions.FirstOrDefault(s => s.Id == booking.SessionId)?.Display ?? string.Empty,
                CubicleNumber = ba?.CubicleId.HasValue == true
                    ? cubicles.FirstOrDefault(c => c.Id == ba.CubicleId)?.Name ?? string.Empty
                    : string.Empty,
                SupervisorId = ba?.SupervisorId,
            };

            return ApiResult<Encounter>.Ok(e);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Draft ─────────────────────────────────────────────────────────
    public async Task<ApiResult<Encounter>> SaveDraftAsync(Encounter encounter)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<Encounter>.Fail("Not signed in.");

            encounter.StudentId = uid;
            encounter.Status    = EncounterStatuses.Draft;

            await FillBookingContextAsync(encounter);

            var r = await _supabase.From<Encounter>().Upsert(encounter);
            return ApiResult<Encounter>.Ok(r.Models.First());
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Submit ────────────────────────────────────────────────────────
    public async Task<ApiResult<Encounter>> SubmitEncounterAsync(Encounter encounter)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<Encounter>.Fail("Not signed in.");

            encounter.StudentId = uid;

            bool isOffsite = encounter.EncounterType == EncounterTypes.Offsite;

            if (isOffsite &&
                (string.IsNullOrWhiteSpace(encounter.OffSiteSupervisorName) ||
                 string.IsNullOrWhiteSpace(encounter.OffSiteOpNumber)))
                return ApiResult<Encounter>.Fail(
                    "Offsite encounters require a supervising optometrist name and OP/practice number.");

            encounter.Status    = isOffsite ? EncounterStatuses.Approved : EncounterStatuses.Submitted;
            encounter.IsLocked  = isOffsite;

            if (isOffsite)
                encounter.SignedOffAt = DateTime.UtcNow;

            await FillBookingContextAsync(encounter);

            // Keep a service-level guard even though the form validates this too.
            // This protects API callers beyond EncounterFormViewModel.
            if (!isOffsite && !encounter.SupervisorId.HasValue)
                return ApiResult<Encounter>.Fail(
                    "Please select a supervisor for this onsite encounter before submitting.");

            var r = await _supabase.From<Encounter>().Upsert(encounter);
            var saved = r.Models.First();

            if (isOffsite)
            {
                // Offsite = auto-approved → insert PoE entries immediately
                await InsertPoeEntriesAsync(saved);
            }
            else
            {
                // Onsite → notify the selected/linked supervisor
                await NotifySupervisorAsync(saved,
                    "New Encounter",
                    "A new encounter has been submitted and requires your review.");
            }

            return ApiResult<Encounter>.Ok(saved);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Resubmit after revision ───────────────────────────────────────
    public async Task<ApiResult<Encounter>> ResubmitEncounterAsync(Encounter encounter)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<Encounter>.Fail("Not signed in.");

            // Re-assert ownership so a student cannot resubmit another student's encounter
            encounter.StudentId          = uid;
            encounter.Status             = EncounterStatuses.Submitted;
            encounter.SupervisorFeedback = null;

            var r    = await _supabase.From<Encounter>().Upsert(encounter);
            var saved = r.Models.First();

            await NotifySupervisorAsync(saved,
                "Encounter Resubmitted",
                $"A student has resubmitted encounter for patient '{saved.PatientName}' after revision. Please review.");

            return ApiResult<Encounter>.Ok(saved);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Attachments ───────────────────────────────────────────────────
    public async Task<ApiResult<EncounterAttachment>> UploadAttachmentAsync(
        Guid encounterId, string fileName, Stream fileStream, string mimeType)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<EncounterAttachment>.Fail("Not signed in.");

            // Upload to Supabase Storage
            var storagePath = $"{uid}/{encounterId}/{fileName}";
            var bytes = new byte[fileStream.Length];
            await fileStream.ReadAsync(bytes, 0, bytes.Length);

            await _supabase.Storage
                .From(SupabaseBuckets.EncounterAttachments)
                .Upload(bytes, storagePath,
                    new Supabase.Storage.FileOptions { ContentType = mimeType, Upsert = true });

            // Insert DB row
            var attachment = new EncounterAttachment
            {
                Id           = Guid.NewGuid(),
                EncounterId  = encounterId,
                FileName     = fileName,
                StoragePath  = storagePath,
                MimeType     = mimeType,
                SizeBytes    = bytes.Length,
            };
            var r = await _supabase.From<EncounterAttachment>().Insert(attachment);
            return ApiResult<EncounterAttachment>.Ok(r.Models.First());
        }
        catch (Exception ex)
        {
            return ApiResult<EncounterAttachment>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<bool>> DeleteAttachmentAsync(
        Guid attachmentId, string storagePath)
    {
        try
        {
            await _supabase.Storage
                .From(SupabaseBuckets.EncounterAttachments)
                .Remove(new List<string> { storagePath });

            await _supabase.From<EncounterAttachment>()
                .Where(a => a.Id == attachmentId)
                .Delete();

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<string>> GetAttachmentUrlAsync(string storagePath)
    {
        try
        {
            var url = await _supabase.Storage
                .From(SupabaseBuckets.EncounterAttachments)
                .CreateSignedUrl(storagePath, 60);
            return ApiResult<string>.Ok(url);
        }
        catch (Exception ex) { return ApiResult<string>.Fail(ex.Message); }
    }

    // ── Private helpers ───────────────────────────────────────────────

    /// <summary>
    /// If the encounter has a BookingId, populate CubicleId and SessionId
    /// from the booking_assignments / bookings tables.
    /// </summary>
    /// <summary>
    /// Finds the supervisor covering the encounter's cubicle and sends them a notification.
    /// Falls back silently — a notification failure must never fail the submit.
    /// </summary>
    private async Task NotifySupervisorAsync(Encounter encounter, string title, string message)
    {
        try
        {
            if (encounter.SupervisorId.HasValue)
            {
                await _notifications.SendToUserAsync(
                    encounter.SupervisorId.Value, title, message, "encounter_submitted");
                return;
            }

            if (!encounter.CubicleId.HasValue) return;

            // Find supervisor assigned to this cubicle via supervisor_cubicles join
            var supCub = await _supabase.From<Models.Admin.SupervisorCubicle>()
                .Where(sc => sc.CubicleId == encounter.CubicleId.Value).Single();
            if (supCub is null) return;

            var sup = await _supabase.From<Models.Admin.SupervisorProfile>()
                .Where(s => s.Id == supCub.SupervisorId).Single();
            if (sup is null) return;

            await _notifications.SendToUserAsync(
                sup.UserId, title, message, "encounter_submitted");
        }
        catch { /* notification failure must not fail the save */ }
    }

    private async Task FillBookingContextAsync(Encounter encounter)
    {
        if (!encounter.BookingId.HasValue) return;
        try
        {
            var booking = await _supabase.From<Booking>()
                .Where(b => b.Id == encounter.BookingId.Value).Single();
            if (booking is not null)
                encounter.SessionId = booking.SessionId;

            var ba = await _supabase.From<BookingAssignment>()
                .Where(a => a.BookingId == encounter.BookingId.Value).Single();
            if (ba is not null)
            {
                encounter.CubicleId = ba.CubicleId;
                encounter.SupervisorId = ba.SupervisorId;
            }
        }
        catch { /* non-critical — do not fail the save */ }
    }

    private async Task InsertPoeEntriesAsync(Encounter encounter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encounter.PoeCategoriesJson)) return;

            var categoryNames = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(encounter.PoeCategoriesJson) ?? new();

            if (!categoryNames.Any()) return;

            // Don't double-credit — check existing entries for this encounter
            var existing = await _supabase.From<PoeEntry>()
                .Where(e => e.EncounterId == encounter.Id).Get();
            if (existing.Models.Any()) return;

            var allCats = (await _supabase.From<PoeCategory>().Get()).Models;

            var entries = categoryNames
                .Select(name => allCats.FirstOrDefault(c =>
                    c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(cat => cat is not null)
                .Select(cat => new PoeEntry
                {
                    Id          = Guid.NewGuid(),
                    EncounterId = encounter.Id,
                    StudentId   = encounter.StudentId,
                    CategoryId  = cat!.Id,
                    Hours       = (double)PoeConstants.HoursPerEncounter,
                })
                .ToList();

            if (entries.Any())
                await _supabase.From<PoeEntry>().Insert(entries);
        }
        catch { /* PoE entry failure should not fail the submit */ }
    }

    private async Task EnrichAsync(List<Encounter> encounters)
    {
        if (!encounters.Any()) return;
        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;
        var cubicles = (await _supabase.From<Cubicle>().Get()).Models;

        // Fetch all relevant bookings in one round-trip instead of one per encounter (N+1 fix)
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
            if (e.CubicleId.HasValue)
                e.CubicleNumber = cubicles.FirstOrDefault(c => c.Id == e.CubicleId)?.Name ?? string.Empty;

            if (!e.BookingId.HasValue) continue;
            var booking = bookings.FirstOrDefault(b => b.Id == e.BookingId.Value);
            if (booking is null) continue;

            e.ClinicName  = clinics.FirstOrDefault(c => c.Id == booking.ClinicId)?.Name    ?? string.Empty;
            e.SlotDisplay = sessions.FirstOrDefault(s => s.Id == booking.SessionId)?.Display ?? string.Empty;
        }
    }
}
