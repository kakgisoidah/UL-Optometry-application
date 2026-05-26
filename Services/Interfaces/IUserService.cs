// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IUserService.cs
//  Admin user management — create, list, edit, deactivate all roles.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IUserService
{
    // ── Lists ─────────────────────────────────────────────────────────
    /// <summary>
    /// All users from public.profiles.
    /// Pass a role string to filter (e.g. "student").
    /// Pass null for all roles.
    /// </summary>
    Task<ApiResult<List<UserProfile>>> GetAllUsersAsync(string? role = null);

    /// <summary>Single user profile by their auth.users.id.</summary>
    Task<ApiResult<UserProfile>> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// All supervisors with their cubicle assignments loaded.
    /// </summary>
    Task<ApiResult<List<SupervisorProfile>>> GetSupervisorsAsync();

    /// <summary>All students with PoE % loaded.</summary>
    Task<ApiResult<List<StudentProfile>>> GetStudentsAsync();

    // ── Create ────────────────────────────────────────────────────────
    /// <summary>
    /// Admin creates a user for any role.
    /// 1. Uses Supabase Auth Admin API to create the auth user.
    /// 2. Inserts into public.profiles with must_change_password = true.
    /// 3. Inserts into role-specific table (supervisors / students / patients).
    /// 4. For supervisors: inserts supervisor_cubicles rows.
    /// The user is sent a default password by email by Supabase.
    /// </summary>
    Task<ApiResult<UserProfile>> CreateUserAsync(CreateUserRequest request);

    // ── Update ────────────────────────────────────────────────────────
    /// <summary>Update a user's editable profile fields.</summary>
    Task<ApiResult<UserProfile>> UpdateUserAsync(Guid userId, string fullName, string phone);

    /// <summary>
    /// Assign cubicles to a supervisor.
    /// Replaces the existing supervisor_cubicles rows.
    /// Unlimited cubicles per supervisor — no upper bound.
    /// </summary>
    Task<ApiResult<bool>> AssignSupervisorCubiclesAsync(
        Guid supervisorUserId,
        List<int> cubicleIds);

    // ── Deactivate ────────────────────────────────────────────────────
    /// <summary>
    /// Sets is_active = false in public.profiles.
    /// Does NOT delete from auth.users — user can be reactivated.
    /// </summary>
    Task<ApiResult<bool>> DeactivateUserAsync(Guid userId);

    /// <summary>Reactivate a previously deactivated user.</summary>
    Task<ApiResult<bool>> ReactivateUserAsync(Guid userId);

    /// <summary>
    /// Trigger a password reset email via Supabase Auth.
    /// Sets must_change_password = true in public.profiles.
    /// </summary>
    Task<ApiResult<bool>> ResetPasswordAsync(Guid userId);
}
