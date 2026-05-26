namespace UL_Optometry.Views.Patient;

using UL_Optometry.Views.Patient.Booking;
using UL_Optometry.Views.Patient.Encounters;
using UL_Optometry.Views.Patient.Profile;
using UL_Optometry.Views.Auth;

public partial class PatientShell : Shell
{
	public PatientShell()
	{
		InitializeComponent();
        RegisterRoutes();
    }


    private static void RegisterRoutes()
    {
        // ── Booking flow (pushed pages) ──────────────────────────────
        Routing.RegisterRoute(nameof(SelectDatePage), typeof(SelectDatePage));
        Routing.RegisterRoute(nameof(SelectSlotPage), typeof(SelectSlotPage));
        Routing.RegisterRoute(nameof(ConfirmBookingPage), typeof(ConfirmBookingPage));
        Routing.RegisterRoute(nameof(BookingSuccessPage), typeof(BookingSuccessPage));
        Routing.RegisterRoute(nameof(BookingDetailPage), typeof(BookingDetailPage));
        Routing.RegisterRoute(nameof(CancelBookingPage), typeof(CancelBookingPage));

        // ── Encounters ───────────────────────────────────────────────
        Routing.RegisterRoute(nameof(PatientEncounterDetailPage), typeof(PatientEncounterDetailPage));

        // ── Profile pushed pages ─────────────────────────────────────
        Routing.RegisterRoute(nameof(EditPatientProfilePage), typeof(EditPatientProfilePage));
        Routing.RegisterRoute(nameof(PatientNotificationsPage), typeof(PatientNotificationsPage));
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
        Routing.RegisterRoute(nameof(PrivacyPolicyPage), typeof(PrivacyPolicyPage));

        // ── Shared auth pages (accessible from all portals) ──────────
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
    }
}