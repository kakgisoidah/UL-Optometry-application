// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Auth/ForcePasswordChangeViewModel.cs
//  Shown immediately after first login for Admin-created accounts.
//  The user cannot navigate away until they set a new password.
//  On success: clears the must_change_password flag in public.profiles,
//  then routes to the correct portal shell.
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;

namespace UL_Optometry.ViewModels.Auth;

public partial class ForcePasswordChangeViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    [ObservableProperty] private string _newPassword     = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool   _isPasswordVisible        = false;
    [ObservableProperty] private bool   _isConfirmVisible         = false;

    public ForcePasswordChangeViewModel(IAuthService auth)
    {
        _auth = auth;
        Title = "Set Your Password";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
        => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleConfirmVisibility()
        => IsConfirmVisible = !IsConfirmVisible;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (NewPassword.Length < 8)
        {
            SetError("Password must be at least 8 characters.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            SetError("Passwords do not match.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            // Change password via Supabase Auth
            // AuthService also clears must_change_password in public.profiles
            var result = await _auth.ChangePasswordAsync(NewPassword);

            if (!result.Success)
            {
                SetError(result.Error ?? "Could not update password. Please try again.");
                return;
            }

            // Now route to the correct portal shell
            App.RouteToRoleShell(_auth.CurrentRole);
        });
    }
}
