// ════════════════════════════════════════════════════════════════════════
//  Services/Implementations/AuthService.cs
// ════════════════════════════════════════════════════════════════════════

using Supabase;
using Supabase.Gotrue;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Implementations;
public class AuthService : IAuthService
{
    private readonly Supabase.Client _supabase;
    private UserProfile?             _currentProfile;

    public AuthService(Supabase.Client supabase) => _supabase = supabase;

    // ── State ─────────────────────────────────────────────────────────
    public bool        IsLoggedIn         => _supabase.Auth.CurrentSession is not null;
    public UserProfile? CurrentUser       => _currentProfile;
    public UserRole    CurrentRole        => _currentProfile?.Role ?? UserRole.Patient;
    public bool        MustChangePassword => _currentProfile?.MustChangePassword ?? false;
    public string      CurrentUserId      => _supabase.Auth.CurrentUser?.Id ?? string.Empty;

    // ── Sign in ───────────────────────────────────────────────────────
    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignIn(email, password);
            if (session?.User is null)
                return AuthResult.Fail("Invalid email or password.");

            var profile = await FetchProfileAsync(session.User!.Id);

            if (!profile.IsActive)
                return AuthResult.Fail("Your account has been deactivated. Contact the administrator.");

            _currentProfile = profile;
            return AuthResult.Ok(profile);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(CleanError(ex.Message));
        }
    }

    // ── Sign out ──────────────────────────────────────────────────────
    public async Task SignOutAsync()
    {
        try { await _supabase.Auth.SignOut(); }
        catch { /* ignore */ }
        _currentProfile = null;
    }

    // ── Restore session on app start ──────────────────────────────────
    public async Task<bool> RestoreSessionAsync()
    {
        try
        {
            var session = _supabase.Auth.CurrentSession;
            if (session is null) return false;

            // Refresh token if close to expiry
            if (session.ExpiresIn < 300)
                session = await _supabase.Auth.RefreshSession();

            if (session?.User is null) return false;

            var profile = await FetchProfileAsync(session.User!.Id);
            if (profile is null || !profile.IsActive) return false;

            _currentProfile = profile;
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Patient self-registration ─────────────────────────────────────
    public async Task<AuthResult> RegisterPatientAsync(RegisterPatientRequest request)
    {
        try
        {
            // 1. Create Supabase auth user
            var session = await _supabase.Auth.SignUp(
                request.Email.ToLowerInvariant(), request.Password);

            if (session?.User is null)
                return AuthResult.Fail("Registration failed. Please try again.");

            // 2. Insert public.profiles row
            var profile = new UserProfile
            {
                UserId             = Guid.Parse(session.User!.Id),
                FullName           = request.FullName.Trim(),
                Email              = request.Email.ToLowerInvariant(),
                Phone              = request.Phone.Trim(),
                RoleString         = "patient",
                MustChangePassword = false,
                IsActive           = true,
            };
            await _supabase.From<UserProfile>().Insert(profile);

            // 3. Insert public.patients row
            var patient = new Models.Admin.PatientDbProfile
            {
                UserId      = Guid.Parse(session.User.Id),
                IdNumber    = request.IdNumber,
                DateOfBirth = request.DateOfBirth,
                Gender      = request.Gender,
            };
            await _supabase.From<Models.Admin.PatientDbProfile>().Insert(patient);

            _currentProfile = profile;
            return AuthResult.Ok(profile);
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(CleanError(ex.Message));
        }
    }

    // ── Forgot password ───────────────────────────────────────────────
    public async Task<ApiResult<bool>> ForgotPasswordAsync(string email)
    {
        try
        {
            await _supabase.Auth.ResetPasswordForEmail(email.ToLowerInvariant());
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail(CleanError(ex.Message));
        }
    }

    // ── Change password (used by ForcePasswordChange + profile pages) ─
    public async Task<ApiResult<bool>> ChangePasswordAsync(string newPassword)
    {
        try
        {
            await _supabase.Auth.Update(new UserAttributes { Password = newPassword });

            // Clear must_change_password flag
            if (_currentProfile is not null)
            {
                await _supabase.From<UserProfile>()
                    .Where(p => p.UserId == _currentProfile.UserId)
                    .Set(p => p.MustChangePassword, false)
                    .Update();

                _currentProfile.MustChangePassword = false;
            }

            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return ApiResult<bool>.Fail(CleanError(ex.Message));
        }
    }

    // ── Private helpers ───────────────────────────────────────────────
    private async Task<UserProfile?> FetchProfileAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        var result = await _supabase
            .From<UserProfile>()
            .Where(p => p.UserId == userGuid)
            .Single();
        return result;
    }

    private static string CleanError(string msg) =>
        msg.Contains("Invalid login") || msg.Contains("invalid_credentials")
            ? "Invalid email or password."
            : msg.Contains("Email not confirmed")
                ? "Please confirm your email address before signing in."
                : msg;
}
