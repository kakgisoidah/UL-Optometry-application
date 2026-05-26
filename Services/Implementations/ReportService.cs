// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/ReportService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class ReportService : IReportService
{
    private readonly Supabase.Client _supabase;
    private readonly IPoeService     _poe;

    public ReportService(Supabase.Client supabase, IPoeService poe)
    {
        _supabase = supabase;
        _poe      = poe;
    }

    public async Task<ApiResult<List<Booking>>> GetBookingReportAsync(
        DateTime? from = null, DateTime? to = null, string? status = null)
    {
        try
        {
            var r = await _supabase.From<Booking>()
                .Order("date", Postgrest.Constants.Ordering.Descending)
                .Get();

            var data = r.Models.AsEnumerable();
            if (status is not null) data = data.Where(b => b.Status == status);
            if (from.HasValue)     data = data.Where(b => b.Date >= from.Value.Date);
            if (to.HasValue)       data = data.Where(b => b.Date <= to.Value.Date);

            return ApiResult<List<Booking>>.Ok(data.ToList());
        }
        catch (Exception ex) { return ApiResult<List<Booking>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<Encounter>>> GetEncounterReportAsync(
        string? status = null, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var r = await _supabase.From<Encounter>()
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var data = r.Models.AsEnumerable();
            if (status is not null) data = data.Where(e => e.Status == status);
            if (from.HasValue)      data = data.Where(e => e.CreatedAt >= from.Value);
            if (to.HasValue)        data = data.Where(e => e.CreatedAt <= to.Value);

            return ApiResult<List<Encounter>>.Ok(data.ToList());
        }
        catch (Exception ex) { return ApiResult<List<Encounter>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<PoeSummary>>> GetPoeReportAsync()
    {
        try
        {
            var students = await _supabase.From<Models.Admin.StudentProfile>().Get();
            var summaries = new List<PoeSummary>();

            foreach (var s in students.Models)
            {
                var r = await _poe.GetPoeSummaryForStudentAsync(s.UserId);
                if (r.Success) summaries.Add(r.Data!);
            }

            return ApiResult<List<PoeSummary>>.Ok(summaries);
        }
        catch (Exception ex) { return ApiResult<List<PoeSummary>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<string>> GetReportPdfUrlAsync(string reportType)
    {
        try
        {
            var response = await _supabase.Functions.Invoke<ReportPdfResponse>(
                "generate-report-pdf",
                _supabase.Auth.CurrentSession?.AccessToken,
                new Supabase.Functions.Client.InvokeFunctionOptions
                {
                    Body = new Dictionary<string, object> { ["reportType"] = reportType }
                });

            return response?.Url is not null
                ? ApiResult<string>.Ok(response.Url)
                : ApiResult<string>.Fail("Report PDF generation failed.");
        }
        catch
        {
            return ApiResult<string>.Fail(
                "Report PDF service not available. Deploy the generate-report-pdf Edge Function.");
        }
    }
}

file record ReportPdfResponse(string? Url);
