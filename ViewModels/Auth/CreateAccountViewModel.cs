// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Auth/CreateAccountViewModel.cs
//  Patient self-registration.
//  Calls IAuthService.RegisterPatientAsync() → Supabase Auth SignUp
//  + inserts into public.profiles and public.patients.
//  On success navigates back to login.
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Models.Auth;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;
using UL_Optometry.Models;

namespace UL_Optometry.ViewModels.Auth;

public partial class CreateAccountViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    // ── Personal info ─────────────────────────────────────────────────
    [ObservableProperty] private string _fullName    = string.Empty;
    [ObservableProperty] private string _email       = string.Empty;
    [ObservableProperty] private string _phone       = string.Empty;
    [ObservableProperty] private string _idNumber    = string.Empty;
    [ObservableProperty] private string _gender      = string.Empty;
    [ObservableProperty] private DateTime _dateOfBirth = DateTime.Today.AddYears(-25);

    // ── Password ──────────────────────────────────────────────────────
    [ObservableProperty] private string _password        = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool   _isPasswordVisible  = false;
    [ObservableProperty] private bool   _isConfirmVisible   = false;

    // ── Success state ─────────────────────────────────────────────────
    [ObservableProperty] private bool _registrationSuccess = false;

    public List<string> GenderOptions { get; } = new() { "Male", "Female", "Other" };

    public CreateAccountViewModel(IAuthService auth)
    {
        _auth = auth;
        Title = "Create Account";
    }

    // ── Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void TogglePasswordVisibility()
        => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmVisibility()
        => IsConfirmVisible = !IsConfirmVisible;

    [RelayCommand]
    private async Task BackToLoginAsync()
        => await Application.Current!.Windows[0].Page!.Navigation.PopAsync();

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // ── Client-side validation ─────────────────────────────────
        if (string.IsNullOrWhiteSpace(FullName))
        { SetError("Full name is required."); return; }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        { SetError("Enter a valid email address."); return; }

        if (Password.Length < 8)
        { SetError("Password must be at least 8 characters."); return; }

        if (Password != ConfirmPassword)
        { SetError("Passwords do not match."); return; }

        if (string.IsNullOrWhiteSpace(Phone))
        { SetError("Phone number is required."); return; }

        await RunBusyAsync(async () =>
        {
            var request = new RegisterPatientRequest
            {
                FullName    = FullName.Trim(),
                Email       = Email.Trim().ToLowerInvariant(),
                Password    = Password,
                Phone       = Phone.Trim(),
                IdNumber    = IdNumber.Trim(),
                DateOfBirth = DateOfBirth,
                Gender      = Gender,
            };

            var result = await _auth.RegisterPatientAsync(request);

            if (!result.Success)
            {
                SetError(result.Error ?? "Registration failed. Please try again.");
                return;
            }

            // Registration succeeded — route straight to Patient shell
            App.RouteToRoleShell(Models.Auth.UserRole.Patient);
        });
    }
}
