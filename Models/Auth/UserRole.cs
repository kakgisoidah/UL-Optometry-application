// ════════════════════════════════════════════════════════════════════════
//  Models/Auth/UserRole.cs
//  Matches the check constraint in public.profiles:
//  role IN ('admin', 'supervisor', 'student', 'patient')
// ════════════════════════════════════════════════════════════════════════

namespace UL_Optometry.Models.Auth;

using Newtonsoft.Json;

using Postgrest.Models;

public enum UserRole
{
    Admin,
    Supervisor,
    Student,
    Patient
}

public static class UserRoleExtensions
{
    /// <summary>Convert enum to the lowercase string stored in Supabase.</summary>
    public static string ToDbString(this UserRole role) => role.ToString().ToLowerInvariant();

    /// <summary>Parse the lowercase DB string back to enum.</summary>
    public static UserRole FromDbString(string s) =>
        s.ToLowerInvariant() switch
        {
            "admin"      => UserRole.Admin,
            "supervisor" => UserRole.Supervisor,
            "student"    => UserRole.Student,
            "patient"    => UserRole.Patient,
            _            => UserRole.Patient
        };
}
