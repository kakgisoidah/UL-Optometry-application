// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IAuditService.cs
//  Immutable audit trail — insert-only, no updates or deletes.
//  Only admin can read via RLS policy.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Common;

namespace UL_Optometry.Services.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// All audit log entries, newest first.
    /// Optional filters: userId, action keyword, date range.
    /// </summary>
    Task<ApiResult<List<AuditLog>>> GetLogsAsync(
        Guid?     userId     = null,
        string?   action     = null,
        DateTime? from       = null,
        DateTime? to         = null,
        int       limit      = 200);

    /// <summary>
    /// Insert an audit entry. Called internally by services
    /// after significant create/update/delete operations.
    /// </summary>
    Task LogAsync(
        string  action,
        string  entity,
        string? entityId = null,
        object? metadata = null);

    /// <summary>
    /// Export all (or filtered) audit logs as a CSV string.
    /// Used by the Export CSV button on AuditPage.
    /// </summary>
    Task<ApiResult<string>> ExportCsvAsync(
        Guid?     userId = null,
        DateTime? from   = null,
        DateTime? to     = null);
}
