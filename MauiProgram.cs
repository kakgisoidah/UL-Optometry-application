using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using UL_Optometry.Services.Implementations;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Admin;
using UL_Optometry.ViewModels.Auth;
using UL_Optometry.ViewModels.Patient;
using UL_Optometry.ViewModels.Student;
using UL_Optometry.ViewModels.Supervisor;
using UL_Optometry.Views.Admin;
using UL_Optometry.Views.Admin.Audit;
using UL_Optometry.Views.Admin.Bookings;
using UL_Optometry.Views.Admin.DashboardPage;
using UL_Optometry.Views.Admin.Encounters;
using UL_Optometry.Views.Admin.Notifications;
using UL_Optometry.Views.Admin.PoE;
using UL_Optometry.Views.Admin.Reports;
using UL_Optometry.Views.Admin.Scheduling;
using UL_Optometry.Views.Admin.Settings;
using UL_Optometry.Views.Admin.Students;
using UL_Optometry.Views.Admin.Supervisors;
using UL_Optometry.Views.Admin.Users;
using UL_Optometry.Views.Auth;
using UL_Optometry.Views.Patient;
using UL_Optometry.Views.Patient.Booking;
using UL_Optometry.Views.Patient.Dashboard;
using UL_Optometry.Views.Patient.Encounters;
using UL_Optometry.Views.Patient.Profile;
using UL_Optometry.Views.Student;
using UL_Optometry.Views.Student.Bookings;
using UL_Optometry.Views.Student.Dashboard;
using UL_Optometry.Views.Student.Encounters;
using UL_Optometry.Views.Student.Notifications;
using UL_Optometry.Views.Student.PoE;
using UL_Optometry.Views.Student.Profile;
using UL_Optometry.Views.Supervisor;
using UL_Optometry.Views.Supervisor.Dashboard;
using UL_Optometry.Views.Supervisor.Notification;
using UL_Optometry.Views.Supervisor.Profile;
using UL_Optometry.Views.Supervisor.ReviewQueue;
using UL_Optometry.Views.Supervisor.SignedOff;
namespace UL_Optometry
{
    public static class MauiProgram
    {


        public static MauiApp CreateMauiAppAsync()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()

                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("DMSans-Regular.ttf", "DMSans");
                    fonts.AddFont("DMSans-Medium.ttf", "DMSansMedium");
                    fonts.AddFont("DMSans-SemiBold.ttf", "DMSansSemiBold");
                    fonts.AddFont("DMSans-Bold.ttf", "DMSansBold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                    {
#if ANDROID
                        handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST
                        handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
                    });
                });

            // ── Configuration (appsettings.json) ─────────────────────────────
            var config = builder.Configuration;
            var supabaseUrl = "https://srgoijwkfgnfwbpbifkk.supabase.co";
            var supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNyZ29pandrZmduZndicGJpZmtrIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzgzNTg2MjYsImV4cCI6MjA5MzkzNDYyNn0.RU4r_Wto6CfgB3k4D59lf7F8gzpGfxhjci3chER4ZW0";
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"]     = supabaseUrl,
                ["Supabase:AnonKey"] = supabaseAnonKey,  // anon key is public — safe in config
            });


          //  var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
          //  builder.Configuration.AddJsonStream(stream);

            // ── Supabase Client (Singleton) ───────────────────────────────────
            builder.Services.AddSingleton(_ =>
                new Supabase.Client(supabaseUrl, supabaseAnonKey, new SupabaseOptions
                {
                    AutoConnectRealtime = true,
                    AutoRefreshToken = true,
                }));


            // ── Shared ────────────────────────────────────────────────────────
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IProfileService, ProfileService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // ── Patient ───────────────────────────────────────────────────────
            builder.Services.AddSingleton<IBookingService, BookingService>();
            builder.Services.AddSingleton<IPatientEncounterService, PatientEncounterService>();

            // ── Student ───────────────────────────────────────────────────────
            builder.Services.AddSingleton<IQueueService, QueueService>();
            builder.Services.AddSingleton<IEncounterService, EncounterService>();
            builder.Services.AddSingleton<IPoeService, PoeService>();

            // ── Supervisor ────────────────────────────────────────────────────
            builder.Services.AddSingleton<IReviewService, ReviewService>();

            // ── Admin ─────────────────────────────────────────────────────────
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<ISchedulingService, SchedulingService>();
            builder.Services.AddSingleton<IAdminBookingService, AdminBookingService>();
            builder.Services.AddSingleton<IAdminEncounterService, AdminEncounterService>();
            builder.Services.AddSingleton<IAuditService, AuditService>();
            builder.Services.AddSingleton<IReportService, ReportService>();

            // ════════════════════════════════════════════════════════════════
            //  VIEWMODELS  (Transient — fresh on each navigation push)
            // ════════════════════════════════════════════════════════════════

            // ── Auth ──────────────────────────────────────────────────────────
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<CreateAccountViewModel>();
            builder.Services.AddTransient<ForgotPasswordViewModel>();
            builder.Services.AddTransient<ForcePasswordChangeViewModel>();

            // ── Patient ───────────────────────────────────────────────────────
            builder.Services.AddTransient<PatientDashboardViewModel>();
            builder.Services.AddTransient<SelectClinicViewModel>();
            builder.Services.AddTransient<SelectDateViewModel>();
            builder.Services.AddTransient<SelectSlotViewModel>();
            builder.Services.AddTransient<ConfirmBookingViewModel>();
            builder.Services.AddTransient<BookingSuccessViewModel>();
            builder.Services.AddTransient<MyBookingsViewModel>();
            builder.Services.AddTransient<BookingDetailViewModel>();
            builder.Services.AddTransient<CancelBookingViewModel>();
            builder.Services.AddTransient<VisitHistoryViewModel>();
            builder.Services.AddTransient<PatientEncounterDetailViewModel>();
            builder.Services.AddTransient<PatientProfileViewModel>();
            builder.Services.AddTransient<EditPatientProfileViewModel>();
            builder.Services.AddTransient<PatientNotificationsViewModel>();
            builder.Services.AddTransient<HelpViewModel>();

            // ── Supervisor ────────────────────────────────────────────────────
            builder.Services.AddTransient<SupervisorDashboardViewModel>();
            builder.Services.AddTransient<ReviewQueueViewModel>();
            builder.Services.AddTransient<EncounterReviewViewModel>();
            builder.Services.AddTransient<ApprovalSuccessViewModel>();
            builder.Services.AddTransient<SignedOffCasesViewModel>();
            builder.Services.AddTransient<SignedOffDetailViewModel>();
            builder.Services.AddTransient<SupNotificationsViewModel>();
            builder.Services.AddTransient<SupProfileViewModel>();

            // ── Student ───────────────────────────────────────────────────────
            builder.Services.AddTransient<StudentDashboardViewModel>();
            builder.Services.AddTransient<StudentBookingsViewModel>();
            builder.Services.AddTransient<StudentBookingDetailViewModel>();
            builder.Services.AddTransient<StudentCancelBookingViewModel>();
            builder.Services.AddTransient<StudentEncountersViewModel>();
            builder.Services.AddTransient<EncounterTypeSelectViewModel>();
            builder.Services.AddTransient<EncounterFormViewModel>();
            builder.Services.AddTransient<EncounterSubmitSuccessViewModel>();
            builder.Services.AddTransient<StudentEncounterDetailViewModel>();
            builder.Services.AddTransient<StudentPoeViewModel>();
            builder.Services.AddTransient<StudentNotificationsViewModel>();
            builder.Services.AddTransient<StudentProfileViewModel>();

            // ── Admin ─────────────────────────────────────────────────────────
            builder.Services.AddTransient<AdminDashboardViewModel>();
            builder.Services.AddTransient<SchedulingViewModel>();
            builder.Services.AddTransient<AdminBookingsViewModel>();
            builder.Services.AddTransient<AdminBookingDetailViewModel>();
            builder.Services.AddTransient<UsersViewModel>();
            builder.Services.AddTransient<AddUserViewModel>();
            builder.Services.AddTransient<UserDetailViewModel>();
            builder.Services.AddTransient<AdminStudentsViewModel>();
            builder.Services.AddTransient<AdminStudentDetailViewModel>();
            builder.Services.AddTransient<AdminSupervisorsViewModel>();
            builder.Services.AddTransient<AdminSupervisorDetailViewModel>();
            builder.Services.AddTransient<AdminEncountersViewModel>();
            builder.Services.AddTransient<AdminEncounterDetailViewModel>();
            builder.Services.AddTransient<AdminPoeViewModel>();
            builder.Services.AddTransient<AdminNotificationsViewModel>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AuditViewModel>();
            builder.Services.AddTransient<AdminCalendarViewModel>();
            builder.Services.AddTransient<AdminCalendarPage>();
            builder.Services.AddTransient<BlockedDatesViewModel>();
            builder.Services.AddTransient<BlockedDatesPage>();
            builder.Services.AddTransient<AdminBookPatientViewModel>();
            builder.Services.AddTransient<AdminBookPatientPage>();

            // ════════════════════════════════════════════════════════════════
            //  PAGES & SHELLS  (Transient)
            // ════════════════════════════════════════════════════════════════

            // ── Auth ──────────────────────────────────────────────────────────
            builder.Services.AddTransient<RoleAwareLoginPage>();
            builder.Services.AddTransient<CreateAccountPage>();
            builder.Services.AddTransient<ForgotPasswordPage>();
            builder.Services.AddTransient<ForcePasswordChangePage>();

            // ── Patient ───────────────────────────────────────────────────────
            builder.Services.AddTransient<PatientShell>();
            builder.Services.AddTransient<PatientDashboardPage>();
            builder.Services.AddTransient<SelectClinicPage>();
            builder.Services.AddTransient<SelectDatePage>();
            builder.Services.AddTransient<SelectSlotPage>();
            builder.Services.AddTransient<ConfirmBookingPage>();
            builder.Services.AddTransient<BookingSuccessPage>();
            builder.Services.AddTransient<MyBookingsPage>();
            builder.Services.AddTransient<BookingDetailPage>();
            builder.Services.AddTransient<CancelBookingPage>();
            builder.Services.AddTransient<VisitHistoryPage>();
            builder.Services.AddTransient<PatientEncounterDetailPage>();
            builder.Services.AddTransient<PatientProfilePage>();
            builder.Services.AddTransient<EditPatientProfilePage>();
            builder.Services.AddTransient<PatientNotificationsPage>();
            builder.Services.AddTransient<HelpPage>();
            builder.Services.AddTransient<PrivacyPolicyPage>();

            // ── Supervisor ────────────────────────────────────────────────────
            builder.Services.AddTransient<SupervisorShell>();
            builder.Services.AddTransient<SupervisorDashboardPage>();
            builder.Services.AddTransient<ReviewQueuePage>();
            builder.Services.AddTransient<EncounterReviewPage>();
            builder.Services.AddTransient<ApprovalSuccessPage>();
            builder.Services.AddTransient<SignedOffCasesPage>();
            builder.Services.AddTransient<SignedOffDetailPage>();
            builder.Services.AddTransient<SupNotificationsPage>();
            builder.Services.AddTransient<SupProfilePage>();

            // ── Student ───────────────────────────────────────────────────────
            builder.Services.AddTransient<StudentShell>();
            builder.Services.AddTransient<StudentDashboardPage>();
            builder.Services.AddTransient<StudentBookingsPage>();
            builder.Services.AddTransient<StudentBookingDetailPage>();
            builder.Services.AddTransient<StudentCancelBookingPage>();
            builder.Services.AddTransient<StudentEncountersPage>();
            builder.Services.AddTransient<EncounterTypeSelectPage>();
            builder.Services.AddTransient<EncounterFormPage>();
            builder.Services.AddTransient<EncounterSubmitSuccessPage>();
            builder.Services.AddTransient<StudentEncounterDetailPage>();
            builder.Services.AddTransient<StudentPoePage>();
            builder.Services.AddTransient<StudentNotificationsPage>();
            builder.Services.AddTransient<StudentProfilePage>();

            // ── Admin ─────────────────────────────────────────────────────────
            builder.Services.AddTransient<AdminShell>();
            builder.Services.AddTransient<AdminDashboardPage>();
            builder.Services.AddTransient<SchedulingPage>();
            builder.Services.AddTransient<AdminBookingsPage>();
            builder.Services.AddTransient<AdminBookingDetailPage>();
            builder.Services.AddTransient<UsersPage>();
            builder.Services.AddTransient<AddUserPage>();
            builder.Services.AddTransient<UserDetailPage>();
            builder.Services.AddTransient<AdminStudentsPage>();
            builder.Services.AddTransient<AdminStudentDetailPage>();
            builder.Services.AddTransient<AdminSupervisorsPage>();
            builder.Services.AddTransient<AdminSupervisorDetailPage>();
            builder.Services.AddTransient<AdminEncountersPage>();
            builder.Services.AddTransient<AdminEncounterDetailPage>();
            builder.Services.AddTransient<AdminPoePage>();
            builder.Services.AddTransient<AdminNotificationsPage>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<AuditPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
