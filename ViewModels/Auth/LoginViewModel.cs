// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Auth/LoginViewModel.cs
//  Single login ViewModel for all four portals.
//  After successful sign-in, reads the JWT role claim and routes
//  the MainPage to the correct shell via App.RouteToRoleShell().
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Constants;
using UL_Optometry.Models.Auth;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;
using UL_Optometry.Views.Auth;

namespace UL_Optometry.ViewModels.Auth;
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;

    // ── Bound properties ──────────────────────────────────────────────
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible = false;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        Title = "Sign In";
    }

    // ── Commands ──────────────────────────────────────────────────────
    [RelayCommand]
    private void TogglePasswordVisibility()
        => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private async Task GoToCreateAccountAsync()
    {
        var page = IPlatformApplication.Current!.Services
                       .GetRequiredService<CreateAccountPage>();
        await Application.Current!.Windows[0].Page!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    private async Task GoToForgotPasswordAsync()
    {
        var page = IPlatformApplication.Current!.Services
                       .GetRequiredService<ForgotPasswordPage>();
        await Application.Current!.Windows[0].Page!.Navigation.PushAsync(page);
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _auth.SignInAsync(Email.Trim(), Password);

            if (!result.Success)
            {
                SetError(result.Error ?? "Sign in failed. Please try again.");
                return;
            }

            // First-login: admin created this account with a default password
            if (result.MustChangePassword)
            {
                App.RouteToForcePasswordChange();
                return;
            }

            // Route to the correct portal shell based on the JWT role
            App.RouteToRoleShell(result.Profile!.Role);
        });
    }

    private bool CanLogin()
        => !string.IsNullOrWhiteSpace(Email) &&
           !string.IsNullOrWhiteSpace(Password);
}
