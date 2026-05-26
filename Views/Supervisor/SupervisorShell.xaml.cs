
using UL_Optometry.Views.Auth;
using UL_Optometry.Views.Supervisor.ReviewQueue;
using UL_Optometry.Views.Supervisor.SignedOff;

namespace UL_Optometry.Views.Supervisor;

public partial class SupervisorShell : Shell
{
	public SupervisorShell()
	{
		InitializeComponent();
        RegisterRoutes();

    }
    private static void RegisterRoutes()
    {
        // ── Review queue pushed pages ────────────────────────────────
        Routing.RegisterRoute(nameof(EncounterReviewPage), typeof(EncounterReviewPage));
        Routing.RegisterRoute(nameof(ApprovalSuccessPage), typeof(ApprovalSuccessPage));

        // ── Signed-off pushed pages ──────────────────────────────────
        Routing.RegisterRoute(nameof(SignedOffDetailPage), typeof(SignedOffDetailPage));

        // ── Shared auth ──────────────────────────────────────────────
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
    }
}