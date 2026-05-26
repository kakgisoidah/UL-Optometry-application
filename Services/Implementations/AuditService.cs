// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/AuditService.cs
// ════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.Json;
using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;
public class AuditService : IAuditService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public AuditService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    public async Task<ApiResult<List<AuditLog>>> GetLogsAsync(
        Guid?     userId = null,
        string?   action = null,
        DateTime? from   = null,
        DateTime? to     = null,
        int       limit  = 200)
    {
        try
        {
            var query = _supabase.From<AuditLog>();

            var r = await query
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            var logs = r.Models.AsEnumerable();

            if (userId.HasValue)
                logs = logs.Where(l => l.UserId == userId);
            if (!string.IsNullOrWhiteSpace(action))
                logs = logs.Where(l => l.Action.Contains(action,
                    StringComparison.OrdinalIgnoreCase));
            if (from.HasValue)
                logs = logs.Where(l => l.CreatedAt >= from.Value);
            if (to.HasValue)
                logs = logs.Where(l => l.CreatedAt <= to.Value);

            // Enrich with user names
            var profiles = (await _supabase.From<UserProfile>().Get()).Models;
            var result   = logs.ToList();

            foreach (var log in result)
            {
                if (log.UserId.HasValue)
                {
                    var prof = profiles.FirstOrDefault(p => p.UserId == log.UserId.Value);
                    log.UserName = prof?.FullName    ?? string.Empty;
                    log.UserRole = prof?.RoleString  ?? string.Empty;
                }
            }

            return ApiResult<List<AuditLog>>.Ok(result);
        }
        catch (Exception ex)
        {
            return ApiResult<List<AuditLog>>.Fail(ex.Message);
        }
    }

    public async Task LogAsync(
        string  action,
        string  entity,
        string? entityId = null,
        object? metadata = null)
    {
        try
        {
            Guid.TryParse(_auth.CurrentUserId, out var uid);

            var log = new AuditLog
            {
                UserId   = uid == Guid.Empty ? null : uid,
                Action   = action,
                Entity   = entity,
                EntityId = entityId,
                Metadata = metadata is not null
                    ? JsonSerializer.Serialize(metadata) : null,
            };

            await _supabase.From<AuditLog>().Insert(log);
        }
        catch { /* audit failure must never crash the app */ }
    }

    public async Task<ApiResult<string>> ExportCsvAsync(
        Guid?     userId = null,
        DateTime? from   = null,
        DateTime? to     = null)
    {
        try
        {
            var result = await GetLogsAsync(userId, null, from, to, 5000);
            if (!result.Success)
                return ApiResult<string>.Fail(result.Error!);

            var sb = new StringBuilder();
            sb.AppendLine("ID,Timestamp,User,Role,Action,Entity,EntityID");

            foreach (var log in result.Data!)
            {
                sb.AppendLine(
                    $"{log.Id}," +
                    $"{log.TimeDisplay}," +
                    $"\"{log.UserName}\"," +
                    $"{log.UserRole}," +
                    $"\"{log.Action}\"," +
                    $"{log.Entity}," +
                    $"{log.EntityId}");
            }

            return ApiResult<string>.Ok(sb.ToString());
        }
        catch (Exception ex)
        {
            return ApiResult<string>.Fail(ex.Message);
        }
    }
}
