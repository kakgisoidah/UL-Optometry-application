// ════════════════════════════════════════════════════════════════════════
//  Models/Auth/AuthModels.cs
//  Request and response models for the Auth service layer.
// ════════════════════════════════════════════════════════════════════════
namespace UL_Optometry.Models.Auth;

// ── Login ─────────────────────────────────────────────────────────────────
public class LoginRequest
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// ── Patient self-registration ─────────────────────────────────────────────
public class RegisterPatientRequest
{
    public string   FullName    { get; set; } = string.Empty;
    public string   Email       { get; set; } = string.Empty;
    public string   Password    { get; set; } = string.Empty;
    public string   Phone       { get; set; } = string.Empty;
    public string   IdNumber    { get; set; } = string.Empty;   // SA ID or passport
    public DateTime DateOfBirth { get; set; }
    public string   Gender      { get; set; } = string.Empty;   // Male / Female / Other
}

// ── Admin creates user (any role) ─────────────────────────────────────────
public class CreateUserRequest
{
    public string   FullName      { get; set; } = string.Empty;
    public string   Email         { get; set; } = string.Empty;
    public string   DefaultPassword { get; set; } = string.Empty;
    public string   Phone         { get; set; } = string.Empty;
    public UserRole Role          { get; set; }

    // Supervisor-specific
    public string OpNumber        { get; set; } = string.Empty;
    public string Qualification   { get; set; } = string.Empty;
    public List<int> CubicleIds   { get; set; } = new();

    // Student-specific
    public string StudentNumber   { get; set; } = string.Empty;
    public int    YearOfStudy     { get; set; }

    // Patient-specific
    public string   IdNumber      { get; set; } = string.Empty;
    public DateTime DateOfBirth   { get; set; }
    public string   Gender        { get; set; } = string.Empty;
}

// ── Password change ───────────────────────────────────────────────────────
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword     { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(NewPassword) &&
        NewPassword.Length >= 8 &&
        NewPassword == ConfirmPassword;
}

// ── Auth result (returned after sign-in / register) ──────────────────────
public class AuthResult
{
    public UserProfile? Profile        { get; set; }
    public bool         Success        { get; set; }
    public string?      Error          { get; set; }
    public bool         MustChangePassword => Profile?.MustChangePassword ?? false;

    public static AuthResult Ok(UserProfile profile)
        => new() { Profile = profile, Success = true };

    public static AuthResult Fail(string error)
        => new() { Success = false, Error = error };
}
