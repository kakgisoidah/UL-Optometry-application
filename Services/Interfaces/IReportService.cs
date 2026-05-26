// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IReportService.cs
//  Admin report data queries. PDF generation is handled by a
//  Supabase Edge Function — these methods return the data for
//  in-app display and the signed PDF download URL.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;


namespace UL_Optometry.Services.Interfaces;

public interface IReportService
{
    /// <summary>Bookings report data — optionally filtered by date range.</summary>
    Task<ApiResult<List<Booking>>> GetBookingReportAsync(
        DateTime? from = null,
        DateTime? to   = null,
        string?   status = null);

    /// <summary>Encounter report data — optionally filtered by status.</summary>
    Task<ApiResult<List<Encounter>>> GetEncounterReportAsync(
        string?   status = null,
        DateTime? from   = null,
        DateTime? to     = null);

    /// <summary>PoE summary for all students.</summary>
    Task<ApiResult<List<PoeSummary>>> GetPoeReportAsync();

    /// <summary>
    /// Request a PDF download URL for a given report type.
    /// Calls a Supabase Edge Function which generates the PDF
    /// and returns a signed Storage URL.
    /// reportType: "bookings" | "encounters" | "poe" | "students" | "supervisors" | "audit"
    /// </summary>
    Task<ApiResult<string>> GetReportPdfUrlAsync(string reportType);
}
