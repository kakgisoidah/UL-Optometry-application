// ════════════════════════════════════════════════════════════════════════
//  Constants/AppRoutes.cs
//  All Shell route name constants.
//  Using nameof() prevents typos — if a class is renamed the compiler
//  catches the broken reference immediately.
//
//  Usage:
//    await Shell.Current.GoToAsync(AppRoutes.BookingDetail);
//    await Shell.Current.GoToAsync($"{AppRoutes.BookingDetail}?bookingId={id}");
//    await Shell.Current.GoToAsync($"//{AppRoutes.MyBookings}");
// ════════════════════════════════════════════════════════════════════════

namespace UL_Optometry.Constants;

public static class AppRoutes
{
    // ── Shared Auth ────────────────────────────────────────────────────
    public const string Login              = "RoleAwareLoginPage";
    public const string CreateAccount      = "CreateAccountPage";
    public const string ForgotPassword     = "ForgotPasswordPage";
    public const string ForcePasswordChange= "ForcePasswordChangePage";

    // ════════════════════════════════════════════════════════
    //  PATIENT  routes
    // ════════════════════════════════════════════════════════

    // Tab routes (absolute // navigation)
    public const string PatientDashboard   = "PatientDashboardPage";
    public const string SelectClinic       = "SelectClinicPage";
    public const string MyBookings         = "MyBookingsPage";
    public const string VisitHistory       = "VisitHistoryPage";
    public const string PatientProfile     = "PatientProfilePage";

    // Pushed routes
    public const string SelectDate         = "SelectDatePage";
    public const string SelectSlot         = "SelectSlotPage";
    public const string ConfirmBooking     = "ConfirmBookingPage";
    public const string BookingSuccess     = "BookingSuccessPage";
    public const string BookingDetail      = "BookingDetailPage";
    public const string CancelBooking      = "CancelBookingPage";
    public const string PatientEncounterDetail = "PatientEncounterDetailPage";
    public const string EditPatientProfile = "EditPatientProfilePage";
    public const string PatientNotifications = "PatientNotificationsPage";
    public const string Help               = "HelpPage";
    public const string PrivacyPolicy      = "PrivacyPolicyPage";

    // ════════════════════════════════════════════════════════
    //  SUPERVISOR  routes
    // ════════════════════════════════════════════════════════

    // Tab routes
    public const string SupervisorDashboard = "SupervisorDashboardPage";
    public const string ReviewQueue         = "ReviewQueuePage";
    public const string SignedOffCases      = "SignedOffCasesPage";
    public const string SupNotifications    = "SupNotificationsPage";
    public const string SupProfile          = "SupProfilePage";

    // Pushed routes
    public const string EncounterReview     = "EncounterReviewPage";
    public const string ApprovalSuccess     = "ApprovalSuccessPage";
    public const string SignedOffDetail     = "SignedOffDetailPage";

    // ════════════════════════════════════════════════════════
    //  STUDENT  routes
    // ════════════════════════════════════════════════════════

    // Tab routes
    public const string StudentDashboard    = "StudentDashboardPage";
    public const string StudentBookings     = "StudentBookingsPage";
    public const string StudentEncounters   = "StudentEncountersPage";
    public const string StudentPoe          = "StudentPoePage";
    public const string StudentProfile      = "StudentProfilePage";

    // Pushed routes
    public const string StudentBookingDetail    = "StudentBookingDetailPage";
    public const string StudentCancelBooking    = "StudentCancelBookingPage";
    public const string EncounterTypeSelect     = "EncounterTypeSelectPage";
    public const string EncounterForm           = "EncounterFormPage";
    public const string EncounterSubmitSuccess  = "EncounterSubmitSuccessPage";
    public const string StudentEncounterDetail  = "StudentEncounterDetailPage";
    public const string StudentNotifications    = "StudentNotificationsPage";

    // ════════════════════════════════════════════════════════
    //  ADMIN  routes
    // ════════════════════════════════════════════════════════

    // Tab / nav routes
    public const string AdminDashboard      = "AdminDashboardPage";
    public const string Scheduling          = "SchedulingPage";
    public const string AdminBookings       = "AdminBookingsPage";
    public const string BookForPatient      = "AdminBookPatientPage";
    public const string Users               = "UsersPage";
    public const string AdminStudents       = "AdminStudentsPage";
    public const string AdminSupervisors    = "AdminSupervisorsPage";
    public const string AdminEncounters     = "AdminEncountersPage";
    public const string AdminPoe            = "AdminPoePage";
    public const string AdminNotifications  = "AdminNotificationsPage";
    public const string Reports             = "ReportsPage";
    public const string Settings            = "SettingsPage";
    public const string Audit               = "AuditPage";

    // Pushed routes
    public const string AdminBookingDetail      = "AdminBookingDetailPage";
    public const string AddUser                 = "AddUserPage";
    public const string UserDetail              = "UserDetailPage";
    public const string AdminStudentDetail      = "AdminStudentDetailPage";
    public const string AdminSupervisorDetail   = "AdminSupervisorDetailPage";
    public const string AdminEncounterDetail    = "AdminEncounterDetailPage";
}
