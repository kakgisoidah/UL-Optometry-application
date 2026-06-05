using UL_Optometry.Constants;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.Views.Admin;
using UL_Optometry.Views.Admin.Bookings;
using UL_Optometry.Views.Admin.Encounters;
using UL_Optometry.Views.Admin.Students;
using UL_Optometry.Views.Admin.Supervisors;
using UL_Optometry.Views.Admin.Users;
using UL_Optometry.Views.Auth;
namespace UL_Optometry.Views.Admin;

public partial class AdminShell : Shell
{
	private readonly IAuthService _auth;

	public AdminShell(IAuthService auth)
	{
		_auth = auth;
		InitializeComponent();
		RegisterRoutes();
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		bool confirmed = await DisplayAlert(
			"Log Out", "Are you sure you want to log out?", "Log Out", "Cancel");
		if (!confirmed) return;
		await _auth.SignOutAsync();
		App.RouteToLogin();
	}

    private static void RegisterRoutes()
    {
        // ── Booking pushed pages ─────────────────────────────────────
        Routing.RegisterRoute(nameof(AdminBookingDetailPage), typeof(AdminBookingDetailPage));

        // ── Users pushed pages ───────────────────────────────────────
        Routing.RegisterRoute(nameof(AddUserPage), typeof(AddUserPage));
        Routing.RegisterRoute(nameof(UserDetailPage), typeof(UserDetailPage));

        // ── Students pushed pages ────────────────────────────────────
        Routing.RegisterRoute(nameof(AdminStudentDetailPage), typeof(AdminStudentDetailPage));

        // ── Supervisors pushed pages ─────────────────────────────────
        Routing.RegisterRoute(nameof(AdminSupervisorDetailPage), typeof(AdminSupervisorDetailPage));

        // ── Encounters pushed pages ──────────────────────────────────
        Routing.RegisterRoute(nameof(AdminEncounterDetailPage), typeof(AdminEncounterDetailPage));

        // ── Admin book patient pages ───────────────────────────────────
        Routing.RegisterRoute(AppRoutes.AdminBookPatient, typeof(AdminBookPatientPage));

        // ── Shared auth ──────────────────────────────────────────────
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
    }
}