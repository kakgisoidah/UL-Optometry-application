using UL_Optometry.Views.Auth;

namespace UL_Optometry.Views.Student;

using UL_Optometry.Views.Auth;
using UL_Optometry.Views.Student.Bookings;
using UL_Optometry.Views.Student.Encounters;
using UL_Optometry.Views.Student.Notifications;


public partial class StudentShell : Shell
{
	public StudentShell()
	{
		InitializeComponent();
        RegisterRoutes();

    }

    private static void RegisterRoutes()
    {
        // ── Booking pushed pages ─────────────────────────────────────
        Routing.RegisterRoute(nameof(StudentBookingDetailPage), typeof(StudentBookingDetailPage));
        Routing.RegisterRoute(nameof(StudentCancelBookingPage), typeof(StudentCancelBookingPage));

        // ── Encounter pushed pages ───────────────────────────────────
        Routing.RegisterRoute(nameof(EncounterTypeSelectPage), typeof(EncounterTypeSelectPage));
        Routing.RegisterRoute(nameof(EncounterFormPage), typeof(EncounterFormPage));
        Routing.RegisterRoute(nameof(EncounterSubmitSuccessPage), typeof(EncounterSubmitSuccessPage));
        Routing.RegisterRoute(nameof(StudentEncounterDetailPage), typeof(StudentEncounterDetailPage));

        // ── Notifications ────────────────────────────────────────────
        Routing.RegisterRoute(nameof(StudentNotificationsPage), typeof(StudentNotificationsPage));

        // ── Shared auth ──────────────────────────────────────────────
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
    }
}