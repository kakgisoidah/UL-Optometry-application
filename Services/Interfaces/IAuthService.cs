// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IAuthService.cs
//  Single auth contract used by all four portals.
//  Implemented by AuthService which wraps supabase-csharp.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Models;
namespace UL_Optometry.Services.Interfaces;

public interface IAuthService
{
    // ── State ─────────────────────────────────────────────────────────
    bool        IsLoggedIn         { get; }
    UserProfile? CurrentUser       { get; }
    UserRole    CurrentRole        { get; }
    bool        MustChangePassword { get; }
    string      CurrentUserId      { get; }   // auth.users.id as string

    // ── Sign-in / out ─────────────────────────────────────────────────
    /// <summary>
    /// Signs in with email + password via Supabase Auth.
    /// Fetches the user's profile from public.profiles after sign-in.
    /// </summary>
    Task<AuthResult> SignInAsync(string email, string password);

    /// <summary>Signs out and clears the local session.</summary>
    Task SignOutAsync();

    /// <summary>
    /// Refreshes the stored session on app start.
    /// Returns true if a valid cached session was found.
    /// </summary>
    Task<bool> RestoreSessionAsync();

    // ── Patient self-registration ─────────────────────────────────────
    /// <summary>
    /// Creates a Supabase Auth user and a matching public.profiles row.
    /// Only patients self-register; all other roles are created by admin.
    /// </summary>
    Task<AuthResult> RegisterPatientAsync(RegisterPatientRequest request);

    // ── Password management ───────────────────────────────────────────
    /// <summary>Sends a password reset email via Supabase Auth.</summary>
    Task<ApiResult<bool>> ForgotPasswordAsync(string email);

    /// <summary>
    /// Changes password for the currently signed-in user.
    /// On success clears the MustChangePassword flag in public.profiles.
    /// </summary>
    Task<ApiResult<bool>> ChangePasswordAsync(string newPassword);
}
