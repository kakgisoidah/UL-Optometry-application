﻿﻿// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/ReviewService.cs
//  Supervisor encounter review — approve (permanent lock) or request revision.
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;
public class ReviewService : IReviewService
{
    private readonly Supabase.Client      _supabase;
    private readonly IAuthService         _auth;
    private readonly INotificationService _notifications;

    public ReviewService(
        Supabase.Client supabase, IAuthService auth, INotificationService notifications)
    {
        _supabase      = supabase;
        _auth          = auth;
        _notifications = notifications;
    }

    // ── Queue tabs ────────────────────────────────────────────────────
    public async Task<ApiResult<List<Encounter>>> GetPendingQueueAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Encounter>>.Ok(new());

            var cubicleIds = await GetSupervisorCubicleIdsAsync(uid);

            var r = await _supabase.From<Encounter>()
                .Filter("status", Postgrest.Constants.Operator.In,
                    new List<object> { "Submitted", "UnderReview" })
                .Order("created_at", Postgrest.Constants.Ordering.Ascending)
                .Get();

            var filtered = r.Models
                .Where(e => MatchesSupervisorAssignment(e, uid, cubicleIds))
                .ToList();

            await EnrichAsync(filtered);
            return ApiResult<List<Encounter>>.Ok(filtered);
        }
        catch (Exception ex) { return ApiResult<List<Encounter>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<Encounter>>> GetReviewedTodayAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Encounter>>.Ok(new());

            var cubicleIds = await GetSupervisorCubicleIdsAsync(uid);
            var today      = DateTime.Today;

            // Approved today
            var approvedToday = (await _supabase.From<Encounter>()
                .Where(e => e.Status == EncounterStatuses.Approved)
                .Order("signed_off_at", Postgrest.Constants.Ordering.Descending)
                .Get()).Models
                .Where(e => e.SignedOffAt?.Date == today &&
                            MatchesSupervisorAssignment(e, uid, cubicleIds))
                .ToList();

            // Revision requests sent today (also counts as "reviewed")
            var revisedToday = (await _supabase.From<Encounter>()
                .Where(e => e.Status == EncounterStatuses.Revision)
                .Order("updated_at", Postgrest.Constants.Ordering.Descending)
                .Get()).Models
                .Where(e => e.UpdatedAt.Date == today &&
                            MatchesSupervisorAssignment(e, uid, cubicleIds))
                .ToList();

            var filtered = approvedToday.Concat(revisedToday).ToList();
            await EnrichAsync(filtered);
            return ApiResult<List<Encounter>>.Ok(filtered);
        }
        catch (Exception ex) { return ApiResult<List<Encounter>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<Encounter>>> GetAllSignedOffAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<Encounter>>.Ok(new());

            var cubicleIds = await GetSupervisorCubicleIdsAsync(uid);

            var r = await _supabase.From<Encounter>()
                .Where(e => e.Status == EncounterStatuses.Approved)
                .Order("signed_off_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var filtered = r.Models
                .Where(e => MatchesSupervisorAssignment(e, uid, cubicleIds))
                .ToList();

            await EnrichAsync(filtered);
            return ApiResult<List<Encounter>>.Ok(filtered);
        }
        catch (Exception ex) { return ApiResult<List<Encounter>>.Fail(ex.Message); }
    }

    // ── Detail ────────────────────────────────────────────────────────
    public async Task<ApiResult<Encounter>> GetEncounterForReviewAsync(Guid encounterId)
    {
        try
        {
            var e = await _supabase.From<Encounter>()
                .Where(x => x.Id == encounterId).Single();
            if (e is null)
                return ApiResult<Encounter>.Fail("Encounter not found.");

            // Mark as UnderReview while supervisor has it open
            if (e.Status == EncounterStatuses.Submitted)
            {
                await _supabase.From<Encounter>()
                    .Where(x => x.Id == encounterId)
                    .Set(x => x.Status, EncounterStatuses.UnderReview)
                    .Update();
                e.Status = EncounterStatuses.UnderReview;
            }

            // Load attachments
            var attachments = await _supabase.From<EncounterAttachment>()
                .Where(a => a.EncounterId == encounterId).Get();
            e.Attachments = attachments.Models;

            await EnrichAsync(new List<Encounter> { e });
            return ApiResult<Encounter>.Ok(e);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Approve (permanent lock) ──────────────────────────────────────
    public async Task<ApiResult<Encounter>> ApproveEncounterAsync(
        ApproveEncounterRequest request)
    {
        try
        {
            // Lock + approve
            await _supabase.From<Encounter>()
                .Where(e => e.Id == request.EncounterId)
                .Set(e => e.Status,      EncounterStatuses.Approved)
                .Set(e => e.IsLocked,    true)
                .Set(e => e.SignedOffAt, DateTime.UtcNow)
                .Update();

            // Insert PoE entries for each selected category
            var encounter = await _supabase.From<Encounter>()
                .Where(e => e.Id == request.EncounterId).Single();

            if (encounter is not null)
                await InsertPoeEntriesAsync(encounter);

            // Mark booking as Completed
            if (encounter?.BookingId.HasValue == true)
            {
                await _supabase.From<Booking>()
                    .Where(b => b.Id == encounter.BookingId!.Value)
                    .Set(b => b.Status, BookingStatuses.Completed)
                    .Update();
            }

            var updated = await _supabase.From<Encounter>()
                .Where(e => e.Id == request.EncounterId).Single();

            // Notify student
            if (updated is not null)
                await NotifyStudentAsync(updated,
                    "Encounter Approved ✅",
                    "Your encounter has been reviewed and signed off by your supervisor.");

            return ApiResult<Encounter>.Ok(updated!);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // ── Request revision ──────────────────────────────────────────────
    public async Task<ApiResult<Encounter>> RequestRevisionAsync(RevisionRequest request)
    {
        try
        {
            await _supabase.From<Encounter>()
                .Where(e => e.Id == request.EncounterId)
                .Set(e => e.Status,             EncounterStatuses.Revision)
                .Set(e => e.SupervisorFeedback, request.Feedback)
                .Update();

            var updated = await _supabase.From<Encounter>()
                .Where(e => e.Id == request.EncounterId).Single();

            if (updated is null)
                return ApiResult<Encounter>.Fail("Encounter not found after revision.");

            await NotifyStudentAsync(updated,
                "Revision Requested \u270F\uFE0F",
                "Your supervisor has requested changes to your encounter. Please review their feedback.");

            return ApiResult<Encounter>.Ok(updated);
        }
        catch (Exception ex) { return ApiResult<Encounter>.Fail(ex.Message); }
    }

    // -- PDF ---
    public async Task<ApiResult<string>> GetPdfUrlAsync(Guid encounterId)
    {
        try
        {
            // Calls a Supabase Edge Function that generates and returns a signed URL
            var response = await _supabase.Functions.Invoke<PdfResponse>(
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
            // Edge function not yet deployed — return placeholder
            return ApiResult<string>.Fail(
                "PDF service not yet available. Deploy the generate-encounter-pdf Edge Function.");
        }
    }

    // ── Dashboard helpers ─────────────────────────────────────────────

    public async Task<ApiResult<int>> GetAssignedCubiclesCountAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid)) return ApiResult<int>.Ok(0);
            var ids = await GetSupervisorCubicleIdsAsync(uid);
            return ApiResult<int>.Ok(ids.Count);
        }
        catch (Exception ex) { return ApiResult<int>.Fail(ex.Message); }
    }

    public async Task<ApiResult<string>> GetCurrentSupervisorOpNumberAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<string>.Fail("Not signed in.");

            var sup = await _supabase.From<SupervisorProfile>()
                .Where(s => s.UserId == uid).Single();

            return sup is not null
                ? ApiResult<string>.Ok(sup.OpNumber)
                : ApiResult<string>.Fail("Supervisor profile not found.");
        }
        catch (Exception ex) { return ApiResult<string>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<ScheduleItem>>> GetTodayScheduleAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<List<ScheduleItem>>.Ok(new());

            var cubicleIds = await GetSupervisorCubicleIdsAsync(uid);
            if (cubicleIds.Count == 0)
                return ApiResult<List<ScheduleItem>>.Ok(new());

            // Today's bookings in the supervisor's cubicles
            var today = DateTime.Today;
            var assignments = (await _supabase.From<BookingAssignment>().Get()).Models
                .Where(a => a.CubicleId.HasValue && cubicleIds.Contains(a.CubicleId!.Value))
                .ToList();

            if (!assignments.Any())
                return ApiResult<List<ScheduleItem>>.Ok(new());

            var bookingIds = assignments.Select(a => a.BookingId).ToList();
            var allBookings = (await _supabase.From<Booking>()
                .Where(b => b.Date == today).Get()).Models
                .Where(b => bookingIds.Contains(b.Id))
                .ToList();

            if (!allBookings.Any())
                return ApiResult<List<ScheduleItem>>.Ok(new());

            var sessions = (await _supabase.From<Session>().Get()).Models;
            var cubicles = (await _supabase.From<Cubicle>().Get()).Models;
            var clinics  = (await _supabase.From<Clinic>().Get()).Models;
            var profiles = (await _supabase.From<UserProfile>().Get()).Models;

            var items = new List<ScheduleItem>();
            foreach (var b in allBookings)
            {
                var assign  = assignments.FirstOrDefault(a => a.BookingId == b.Id);
                var session = sessions.FirstOrDefault(s => s.Id == b.SessionId);
                var cubicle = assign?.CubicleId.HasValue == true
                    ? cubicles.FirstOrDefault(c => c.Id == assign.CubicleId!.Value) : null;
                var clinic  = clinics.FirstOrDefault(c => c.Id == b.ClinicId);
                var student = assign?.StudentId.HasValue == true
                    ? profiles.FirstOrDefault(p => p.UserId == assign.StudentId!.Value) : null;

                items.Add(new ScheduleItem
                {
                    TimeRange   = session is not null ? $"{session.StartTime} – {session.EndTime}" : string.Empty,
                    CubicleName = cubicle?.Name ?? string.Empty,
                    ClinicName  = clinic?.Name  ?? "Clinic",
                    StudentName = student?.FullName ?? string.Empty,
                });
            }

            items = items.OrderBy(i => i.TimeRange).ToList();
            return ApiResult<List<ScheduleItem>>.Ok(items);
        }
        catch (Exception ex) { return ApiResult<List<ScheduleItem>>.Fail(ex.Message); }
    }

    // ── Private helpers ───────────────────────────────────────────────

    /// <summary>
    /// Sends a notification to the student who owns the encounter.
    /// Failures are swallowed — they must not affect the review outcome.
    /// </summary>
    private async Task NotifyStudentAsync(Encounter encounter, string title, string message)
    {
        try
        {
            await _notifications.SendToUserAsync(
                encounter.StudentId, title, message, "encounter_review");
        }
        catch { /* notification failure must not fail the review action */ }
    }

    private async Task<HashSet<int>> GetSupervisorCubicleIdsAsync(Guid supervisorUserId)
    {
        // Look up supervisor record by user_id
        var sup = await _supabase.From<SupervisorProfile>()
            .Where(s => s.UserId == supervisorUserId).Single();
        if (sup is null) return new();

        var supCubs = (await _supabase.From<SupervisorCubicle>()
            .Where(sc => sc.SupervisorId == sup.Id).Get()).Models;
        return supCubs.Select(sc => sc.CubicleId).ToHashSet();
    }

    /// <summary>
    /// Match encounters by explicit supervisor assignment when available.
    /// Falls back to cubicle-based matching for legacy encounters that don't have supervisor_id set.
    /// </summary>
    private static bool MatchesSupervisorAssignment(
        Encounter e,
        Guid supervisorUserId,
        HashSet<int> cubicleIds)
    {
        if (e.SupervisorId.HasValue)
            return e.SupervisorId.Value == supervisorUserId;

        return e.CubicleId.HasValue && cubicleIds.Contains(e.CubicleId.Value);
    }

    private async Task InsertPoeEntriesAsync(Encounter encounter)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encounter.PoeCategoriesJson)) return;

            var categoryNames = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(encounter.PoeCategoriesJson) ?? new();
            if (!categoryNames.Any()) return;

            // Don't double-credit — check existing entries
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
        catch { /* PoE failure must not fail the approval */ }
    }

    private async Task EnrichAsync(List<Encounter> encounters)
    {
        if (!encounters.Any()) return;
        var clinics  = (await _supabase.From<Clinic>().Get()).Models;
        var sessions = (await _supabase.From<Session>().Get()).Models;
        var profiles = (await _supabase.From<UserProfile>().Get()).Models;
        var cubicles = (await _supabase.From<Cubicle>().Get()).Models;

        // Pre-fetch all bookings in one round-trip (N+1 fix)
        var bookingIds = encounters
            .Where(e => e.BookingId.HasValue)
            .Select(e => e.BookingId!.Value)
            .ToHashSet();
        var bookings = bookingIds.Any()
            ? (await _supabase.From<Booking>().Get()).Models
                .Where(b => bookingIds.Contains(b.Id)).ToList()
            : new List<Booking>();

        // Pre-fetch booking assignments only for encounters that need cubicle lookup (N+1 fix)
        var missingCubicleBookingIds = encounters
            .Where(e => !e.CubicleId.HasValue && e.BookingId.HasValue)
            .Select(e => e.BookingId!.Value)
            .ToHashSet();
        var bookingAssignments = missingCubicleBookingIds.Any()
            ? (await _supabase.From<BookingAssignment>().Get()).Models
                .Where(a => missingCubicleBookingIds.Contains(a.BookingId)).ToList()
            : new List<BookingAssignment>();

        foreach (var e in encounters)
        {
            var stuProf = profiles.FirstOrDefault(p => p.UserId == e.StudentId);
            e.StudentName     = stuProf?.FullName    ?? string.Empty;
            e.StudentInitials = stuProf?.Initials    ?? "?";

            if (e.CubicleId.HasValue)
                e.CubicleNumber = cubicles.FirstOrDefault(c => c.Id == e.CubicleId)?.Name ?? string.Empty;

            if (!e.BookingId.HasValue) continue;
            var booking = bookings.FirstOrDefault(b => b.Id == e.BookingId.Value);
            if (booking is null) continue;

            e.ClinicName  = clinics.FirstOrDefault(c => c.Id == booking.ClinicId)?.Name     ?? string.Empty;
            e.SlotDisplay = sessions.FirstOrDefault(s => s.Id == booking.SessionId)?.Display ?? string.Empty;

            if (!e.CubicleId.HasValue)
            {
                var ba = bookingAssignments.FirstOrDefault(a => a.BookingId == e.BookingId.Value);
                if (ba?.CubicleId.HasValue == true)
                    e.CubicleNumber = cubicles.FirstOrDefault(c => c.Id == ba.CubicleId)?.Name ?? string.Empty;
            }
        }
    }
}

/// <summary>Response shape from the generate-encounter-pdf Edge Function.</summary>
file record PdfResponse(string? Url);
