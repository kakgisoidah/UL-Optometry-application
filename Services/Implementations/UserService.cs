// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/UserService.cs
//
//  IMPORTANT — Creating users requires admin privileges.
//  Two approaches are supported:
//
//  OPTION A (secure — recommended for production):
//    Deploy the Supabase Edge Function in /EdgeFunctions/create-user/
//    Set USE_EDGE_FUNCTION = true below.
//    The mobile app calls the Edge Function with the user's JWT.
//    The Edge Function uses the service role key server-side.
//
//  OPTION B (dev/internal only):
//    Set USE_EDGE_FUNCTION = false and provide your service role key
//    in appsettings.json as "Supabase:ServiceRoleKey".
//    NEVER ship Option B to a public app store.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.Models;
using Supabase;
using UL_Optometry.Constants;
using Microsoft.Extensions.Configuration;
namespace UL_Optometry.Services.Implementations;

public class UserService : IUserService
{
    // ── Toggle this when you deploy the Edge Function ─────────────────
    private const bool USE_EDGE_FUNCTION = true;
    // ─────────────────────────────────────────────────────────────────

    private readonly Supabase.Client _supabase;
    private readonly IConfiguration  _config;

    public UserService(Supabase.Client supabase, IConfiguration config)
    {
        _supabase = supabase;
        _config   = config;
    }

    // ── Lists ─────────────────────────────────────────────────────────
    public async Task<ApiResult<List<UserProfile>>> GetAllUsersAsync(string? role = null)
    {
        try
        {
            var r = role is null
                ? await _supabase.From<UserProfile>()
                    .Order("full_name", Postgrest.Constants.Ordering.Ascending).Get()
                : await _supabase.From<UserProfile>()
                    .Where(p => p.RoleString == role)
                    .Order("full_name", Postgrest.Constants.Ordering.Ascending).Get();

            return ApiResult<List<UserProfile>>.Ok(r.Models);
        }
        catch (Exception ex) { return ApiResult<List<UserProfile>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<UserProfile>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var r = await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId).Single();
            return r is not null
                ? ApiResult<UserProfile>.Ok(r)
                : ApiResult<UserProfile>.Fail("User not found.");
        }
        catch (Exception ex) { return ApiResult<UserProfile>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<SupervisorProfile>>> GetSupervisorsAsync()
    {
        try
        {
            var sups     = (await _supabase.From<SupervisorProfile>().Get()).Models;
            var supCubs  = (await _supabase.From<SupervisorCubicle>().Get()).Models;
            var cubicles = (await _supabase.From<Cubicle>().Get()).Models;
            var profiles = (await _supabase.From<UserProfile>().Get()).Models;

            foreach (var s in sups)
            {
                var prof = profiles.FirstOrDefault(p => p.UserId == s.UserId);
                s.FullName = prof?.FullName ?? string.Empty;
                s.Email    = prof?.Email    ?? string.Empty;
                s.IsActive = prof?.IsActive ?? true;

                var cubIds = supCubs.Where(sc => sc.SupervisorId == s.Id)
                    .Select(sc => sc.CubicleId).ToList();
                s.AssignedCubicleIds   = cubIds;
                s.AssignedCubicleNames = cubicles
                    .Where(c => cubIds.Contains(c.Id))
                    .Select(c => c.Name).ToList();
            }

            return ApiResult<List<SupervisorProfile>>.Ok(sups);
        }
        catch (Exception ex) { return ApiResult<List<SupervisorProfile>>.Fail(ex.Message); }
    }

    public async Task<ApiResult<List<StudentProfile>>> GetStudentsAsync()
    {
        try
        {
            var students = (await _supabase.From<StudentProfile>().Get()).Models;
            var profiles = (await _supabase.From<UserProfile>().Get()).Models;
            var poeEntries = (await _supabase.From<PoeEntry>().Get()).Models;
            var poeCats  = (await _supabase.From<PoeCategory>().Get()).Models;

            const double totalRequired = 500.0;

            foreach (var s in students)
            {
                var prof = profiles.FirstOrDefault(p => p.UserId == s.UserId);
                s.FullName = prof?.FullName ?? string.Empty;
                s.Email    = prof?.Email    ?? string.Empty;
                s.IsActive = prof?.IsActive ?? true;

                var logged    = poeEntries.Where(e => e.StudentId == s.UserId).Sum(e => e.Hours);
                s.PoePercent  = Math.Round(Math.Min(logged / totalRequired * 100, 100), 1);
            }

            return ApiResult<List<StudentProfile>>.Ok(students);
        }
        catch (Exception ex) { return ApiResult<List<StudentProfile>>.Fail(ex.Message); }
    }

    // ── Create ────────────────────────────────────────────────────────
    public async Task<ApiResult<UserProfile>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            return USE_EDGE_FUNCTION
                ? await CreateViaEdgeFunctionAsync(request)
                : await CreateViaAdminApiAsync(request);
        }
        catch (Exception ex) { return ApiResult<UserProfile>.Fail(ex.Message); }
    }

    // ── Option A — Edge Function (deploy EdgeFunctions/create-user/) ──
    private async Task<ApiResult<UserProfile>> CreateViaEdgeFunctionAsync(
        CreateUserRequest request)
    {
        var url     = _config["Supabase:Url"]!;
        var anonKey = _config["Supabase:AnonKey"]!;
        var token   = _supabase.Auth.CurrentSession?.AccessToken ?? string.Empty;

        var payload = new Dictionary<string, object?>
        {
            ["email"]         = request.Email,
            ["password"]      = request.DefaultPassword,
            ["fullName"]      = request.FullName,
            ["phone"]         = request.Phone,
            ["role"]          = request.Role.ToDbString(),
            ["opNumber"]      = request.OpNumber,
            ["qualification"] = request.Qualification,
            ["cubicleIds"]    = request.CubicleIds,
            ["studentNumber"] = request.StudentNumber,
            ["yearOfStudy"]   = request.YearOfStudy,
            ["idNumber"]      = request.IdNumber,
            ["gender"]        = request.Gender,
            ["dateOfBirth"]   = request.DateOfBirth.ToString("yyyy-MM-dd"),
        };

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        http.DefaultRequestHeaders.Add("apikey", anonKey);

        var json    = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var resp    = await http.PostAsync($"{url}/functions/v1/create-user", content);
        var raw     = await resp.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(raw))
            return ApiResult<UserProfile>.Fail("Edge Function returned no response.");

        using var doc = System.Text.Json.JsonDocument.Parse(raw);

        if (doc.RootElement.TryGetProperty("error", out var errEl))
            return ApiResult<UserProfile>.Fail(errEl.GetString() ?? "Edge Function failed.");

        if (!doc.RootElement.TryGetProperty("userId", out var userIdEl))
            return ApiResult<UserProfile>.Fail("Edge Function did not return a userId.");

        var userId = userIdEl.GetString();
        if (string.IsNullOrWhiteSpace(userId))
            return ApiResult<UserProfile>.Fail("Edge Function returned empty userId.");

        var profile = await _supabase.From<UserProfile>()
            .Where(p => p.UserId == Guid.Parse(userId)).Single();

        return profile is not null
            ? ApiResult<UserProfile>.Ok(profile)
            : ApiResult<UserProfile>.Fail("User created but profile not found.");
    }

    // ── Option B — Direct Admin API (dev only, needs service role) ────
    private async Task<ApiResult<UserProfile>> CreateViaAdminApiAsync(
        CreateUserRequest request)
    {
        // Build a separate service-role client that bypasses RLS
        var url            = _config["Supabase:Url"]!;
        var serviceRoleKey = _config["Supabase:ServiceRoleKey"]!;

        if (string.IsNullOrWhiteSpace(serviceRoleKey))
            return ApiResult<UserProfile>.Fail(
                "Supabase:ServiceRoleKey is not configured. " +
                "Add it to appsettings.json or use the Edge Function.");

        var adminClient = new Supabase.Client(url, serviceRoleKey,
            new Supabase.SupabaseOptions { AutoConnectRealtime = false });
        await adminClient.InitializeAsync();

        // Get admin client with service role key
        var adminAuth = adminClient.AdminAuth(serviceRoleKey);

        // 1. Create auth user via admin API (service role allows this)
        var adminUser = await adminAuth.CreateUser(
            new Supabase.Gotrue.AdminUserAttributes
            {
                Email        = request.Email.ToLowerInvariant(),
                Password     = request.DefaultPassword,
                EmailConfirm = true,   // skip confirmation email
            });

        if (adminUser is null)
            return ApiResult<UserProfile>.Fail("Failed to create auth user.");

        var uid = Guid.Parse(adminUser.Id!);

        // 2. Insert profiles row (service-role client bypasses RLS)
        var profile = new UserProfile
        {
            UserId             = uid,
            FullName           = request.FullName.Trim(),
            Email              = request.Email.ToLowerInvariant(),
            Phone              = request.Phone.Trim(),
            RoleString         = request.Role.ToDbString(),
            MustChangePassword = true,
            IsActive           = true,
        };
        await adminClient.From<UserProfile>().Insert(profile);

        // 3. Insert role-specific table row
        switch (request.Role)
        {
            case UserRole.Supervisor:
                var sup = new SupervisorProfile
                {
                    Id            = Guid.NewGuid(),
                    UserId        = uid,
                    OpNumber      = request.OpNumber.Trim(),
                    Qualification = request.Qualification.Trim(),
                };
                var supR  = await adminClient.From<SupervisorProfile>().Insert(sup);
                var supId = supR.Models.First().Id;

                if (request.CubicleIds.Any())
                {
                    var cubRows = request.CubicleIds.Select(cid =>
                        new SupervisorCubicle { SupervisorId = supId, CubicleId = cid }).ToList();
                    await adminClient.From<SupervisorCubicle>().Insert(cubRows);
                }
                break;

            case UserRole.Student:
                await adminClient.From<StudentProfile>().Insert(new StudentProfile
                {
                    Id            = Guid.NewGuid(),
                    UserId        = uid,
                    StudentNumber = request.StudentNumber.Trim(),
                    YearOfStudy   = request.YearOfStudy,
                });
                break;

            case UserRole.Patient:
                await adminClient.From<Models.Admin.PatientDbProfile>().Insert(
                    new Models.Admin.PatientDbProfile
                    {
                        Id          = Guid.NewGuid(),
                        UserId      = uid,
                        IdNumber    = request.IdNumber,
                        DateOfBirth = request.DateOfBirth,
                        Gender      = request.Gender,
                    });
                break;
        }

        return ApiResult<UserProfile>.Ok(profile);
    }

    // ── Update ────────────────────────────────────────────────────────
    public async Task<ApiResult<UserProfile>> UpdateUserAsync(
        Guid userId, string fullName, string phone)
    {
        try
        {
            await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId)
                .Set(p => p.FullName, fullName)
                .Set(p => p.Phone,    phone)
                .Update();

            var updated = await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId).Single();
            return ApiResult<UserProfile>.Ok(updated!);
        }
        catch (Exception ex) { return ApiResult<UserProfile>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> AssignSupervisorCubiclesAsync(
        Guid supervisorUserId, List<int> cubicleIds)
    {
        try
        {
            // Get supervisor row
            var sup = await _supabase.From<SupervisorProfile>()
                .Where(s => s.UserId == supervisorUserId).Single();
            if (sup is null)
                return ApiResult<bool>.Fail("Supervisor not found.");

            // Delete existing assignments
            await _supabase.From<SupervisorCubicle>()
                .Where(sc => sc.SupervisorId == sup.Id).Delete();

            // Insert new assignments
            if (cubicleIds.Any())
            {
                var rows = cubicleIds.Select(cid =>
                    new SupervisorCubicle { SupervisorId = sup.Id, CubicleId = cid })
                    .ToList();
                await _supabase.From<SupervisorCubicle>().Insert(rows);
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    // ── Activate / Deactivate ─────────────────────────────────────────
    public async Task<ApiResult<bool>> DeactivateUserAsync(Guid userId)
    {
        try
        {
            await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId)
                .Set(p => p.IsActive, false)
                .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> ReactivateUserAsync(Guid userId)
    {
        try
        {
            await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId)
                .Set(p => p.IsActive, true)
                .Update();
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }

    public async Task<ApiResult<bool>> ResetPasswordAsync(Guid userId)
    {
        try
        {
            var profile = await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId).Single();
            if (profile is null)
                return ApiResult<bool>.Fail("User not found.");

            await _supabase.Auth.ResetPasswordForEmail(profile.Email);

            await _supabase.From<UserProfile>()
                .Where(p => p.UserId == userId)
                .Set(p => p.MustChangePassword, true)
                .Update();

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return ApiResult<bool>.Fail(ex.Message); }
    }
}
