// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Auth/ForgotPasswordViewModel.cs
//  Sends a password-reset email via Supabase Auth.
//  Shared by all portals — reachable from RoleAwareLoginPage.
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;

namespace UL_Optometry.ViewModels.Auth;

public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    [ObservableProperty] private string _email = string.Empty;

    /// <summary>True after the reset link has been sent — shows success state.</summary>
    [ObservableProperty] private bool _sent = false;

    public ForgotPasswordViewModel(IAuthService auth)
    {
        _auth = auth;
        Title = "Reset Password";
    }

    [RelayCommand]
    private async Task SendResetLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            SetError("Enter a valid email address.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var result = await _auth.ForgotPasswordAsync(Email.Trim().ToLowerInvariant());

            if (result.Success)
                Sent = true;
            else
                SetError(result.Error ?? "Could not send reset link. Please try again.");
        });
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
        => await Application.Current!.Windows[0].Page!.Navigation.PopAsync();
}
