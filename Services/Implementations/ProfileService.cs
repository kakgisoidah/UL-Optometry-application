// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/ProfileService.cs
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Models;
using UL_Optometry.Services.Interfaces;

namespace UL_Optometry.Services.Implementations;

public class ProfileService : IProfileService
{
    private readonly Supabase.Client _supabase;
    private readonly IAuthService    _auth;

    public ProfileService(Supabase.Client supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth     = auth;
    }

    public async Task<ApiResult<UserProfile>> GetProfileAsync()
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<UserProfile>.Fail("Not signed in.");

            var profile = await _supabase
                .From<UserProfile>()
                .Where(p => p.UserId == uid)
                .Single();

            return profile is not null
                ? ApiResult<UserProfile>.Ok(profile)
                : ApiResult<UserProfile>.Fail("Profile not found.");
        }
        catch (Exception ex)
        {
            return ApiResult<UserProfile>.Fail(ex.Message);
        }
    }

    public async Task<ApiResult<UserProfile>> UpdateProfileAsync(string fullName, string phone)
    {
        try
        {
            if (!Guid.TryParse(_auth.CurrentUserId, out var uid))
                return ApiResult<UserProfile>.Fail("Not signed in.");

            await _supabase
                .From<UserProfile>()
                .Where(p => p.UserId == uid)
                .Set(p => p.FullName, fullName)
                .Set(p => p.Phone,    phone)
                .Update();

            var updated = await _supabase
                .From<UserProfile>()
                .Where(p => p.UserId == uid)
                .Single();

            return ApiResult<UserProfile>.Ok(updated!);
        }
        catch (Exception ex)
        {
            return ApiResult<UserProfile>.Fail(ex.Message);
        }
    }
}
