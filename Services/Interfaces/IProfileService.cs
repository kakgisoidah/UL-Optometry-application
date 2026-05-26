// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IProfileService.cs
//  Shared profile reads/writes used by all portals' Profile pages.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IProfileService
{
    /// <summary>Fetch the current user's profile from public.profiles.</summary>
    Task<ApiResult<UserProfile>> GetProfileAsync();

    /// <summary>
    /// Update editable fields: FullName, Phone.
    /// Email changes go through Supabase Auth separately.
    /// </summary>
    Task<ApiResult<UserProfile>> UpdateProfileAsync(
        string fullName,
        string phone);
}
