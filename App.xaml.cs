// ════════════════════════════════════════════════════════════════════════
//  App.xaml.cs
//  Entry point. On startup: tries to restore a cached Supabase session.
//  If no session → RoleAwareLoginPage.
//  If session + MustChangePassword → ForcePasswordChangePage.
//  If session + role → correct portal shell.
//
//  Static helpers (RouteToRoleShell, RouteToForcePasswordChange) are
//  called by LoginViewModel and ForcePasswordChangeViewModel after
//  async operations complete.
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Auth;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.Views.Admin;
using UL_Optometry.Views.Auth;
using UL_Optometry.Views.Patient;
using UL_Optometry.Views.Student;
using UL_Optometry.Views.Supervisor;
namespace UL_Optometry;

    public partial class App : Application
    {
    private readonly IAuthService _auth;
    private readonly IServiceProvider _services;

    public App(IAuthService auth, IServiceProvider services)
        {
            InitializeComponent();
        _auth = auth;
        _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
        // Try to restore a cached session from SecureStorage on app start
        _ = TryRestoreSessionAsync();

        // While waiting for session restore show login —
        // session restore will swap MainPage if successful
        return new Window(new NavigationPage(_services.GetRequiredService<RoleAwareLoginPage>()));
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var restored = await _auth.RestoreSessionAsync();
            if (!restored) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_auth.MustChangePassword)
                    RouteToForcePasswordChange();
                else
                    RouteToRoleShell(_auth.CurrentRole);
            });
        }
        catch
        {
            // Session restore failed — user stays on login page
        }
    }

    // ── Static routing helpers ────────────────────────────────────────
    // Called from ViewModels via App.RouteToRoleShell(role)

    /// <summary>
    /// Route to the correct portal Shell based on the user's role.
    /// Called after login, after session restore, and after force password change.
    /// </summary>
    public static void RouteToRoleShell(UserRole role)
    {
        var services = IPlatformApplication.Current!.Services;

        Page shell = role switch
        {
            UserRole.Admin => services.GetRequiredService<AdminShell>(),
            UserRole.Supervisor => services.GetRequiredService<SupervisorShell>(),
            UserRole.Student => services.GetRequiredService<StudentShell>(),
            UserRole.Patient => services.GetRequiredService<PatientShell>(),
            _ => services.GetRequiredService<RoleAwareLoginPage>()
        };

        if (Current?.Windows.Count > 0)
            Current.Windows[0].Page = shell;
    }

    public static void RouteToForcePasswordChange()
    {
        var services = IPlatformApplication.Current!.Services;
        if (Current?.Windows.Count > 0)
            Current.Windows[0].Page = services.GetRequiredService<ForcePasswordChangePage>();
    }

    public static void RouteToLogin()
    {
        var services = IPlatformApplication.Current!.Services;
        if (Current?.Windows.Count > 0)
            Current.Windows[0].Page = new NavigationPage(services.GetRequiredService<RoleAwareLoginPage>());
    }
}