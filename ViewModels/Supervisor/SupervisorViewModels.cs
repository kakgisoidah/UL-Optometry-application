// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Supervisor/SupervisorViewModels.cs
//  All Supervisor portal ViewModels.
// ════════════════════════════════════════════════════════════════════════

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Notification;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;

namespace UL_Optometry.ViewModels.Supervisor;

// ══════════════════════════════════════════════════════════════
//  1. DASHBOARD
// ══════════════════════════════════════════════════════════════
public partial class SupervisorDashboardViewModel : BaseViewModel
{
    private readonly IReviewService   _review;
    private readonly IAuthService     _auth;

    [ObservableProperty] private int    _pendingCount;
    [ObservableProperty] private int    _reviewedTodayCount;
    [ObservableProperty] private int    _signedOffTodayCount;
    [ObservableProperty] private int    _assignedCubiclesCount;
    [ObservableProperty] private string _supervisorName = string.Empty;
    [ObservableProperty] private string _opNumber       = string.Empty;
    [ObservableProperty] private string _greeting       = string.Empty;
    [ObservableProperty] private string _todayLabel     = string.Empty;

    public ObservableCollection<ScheduleItem> TodaySchedule { get; } = new();

    public SupervisorDashboardViewModel(IReviewService review, IAuthService auth)
    {
        _review = review;
        _auth   = auth;
        Title   = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var hour = DateTime.Now.Hour;
            var greetWord = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
            var lastName = _auth.CurrentUser?.FullName.Split(' ').LastOrDefault() ?? "Supervisor";
            SupervisorName = lastName;
            Greeting  = $"Good {greetWord}, Dr. {lastName}! 👋";
            TodayLabel = $"Your overview for today — {DateTime.Today:dddd, dd MMMM yyyy}";

            var pending = await _review.GetPendingQueueAsync();
            if (pending.Success) PendingCount = pending.Data?.Count ?? 0;

            var today = await _review.GetReviewedTodayAsync();
            if (today.Success) ReviewedTodayCount = today.Data?.Count ?? 0;

            var signedOff = await _review.GetAllSignedOffAsync();
            if (signedOff.Success)
                SignedOffTodayCount = signedOff.Data?
                    .Count(e => e.SignedOffAt?.Date == DateTime.Today) ?? 0;

            var cubCount = await _review.GetAssignedCubiclesCountAsync();
            if (cubCount.Success) AssignedCubiclesCount = cubCount.Data;

            var schedule = await _review.GetTodayScheduleAsync();
            TodaySchedule.Clear();
            if (schedule.Success && schedule.Data is not null)
                foreach (var item in schedule.Data)
                    TodaySchedule.Add(item);
        });
    }

    [RelayCommand]
    private async Task GoToReviewQueueAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.ReviewQueue}");

    [RelayCommand]
    private async Task GoToSignedOffAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.SignedOffCases}");

    [RelayCommand]
    private async Task GoToNotificationsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.SupNotifications}");
}

// ══════════════════════════════════════════════════════════════
//  2. REVIEW QUEUE
// ══════════════════════════════════════════════════════════════
public partial class ReviewQueueViewModel : BaseViewModel
{
    private readonly IReviewService _review;

    private readonly List<Encounter> _allPending       = new();
    public ObservableCollection<Encounter> Pending       { get; } = new();
    public ObservableCollection<Encounter> ReviewedToday { get; } = new();
    public ObservableCollection<Encounter> AllSignedOff  { get; } = new();

    [ObservableProperty] private int    _pendingCount;
    [ObservableProperty] private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;
        Pending.Clear();
        var results = string.IsNullOrEmpty(q)
            ? _allPending
            : _allPending.Where(e =>
                e.StudentName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.PatientName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.PatientFileNumber.Contains(q, StringComparison.OrdinalIgnoreCase));
        foreach (var e in results) Pending.Add(e);
    }

    public ReviewQueueViewModel(IReviewService review)
    {
        _review = review;
        Title   = "Review Queue";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var pending = await _review.GetPendingQueueAsync();
            if (pending.Success)
            {
                _allPending.Clear();
                _allPending.AddRange(pending.Data!);
                PendingCount = _allPending.Count;
                ApplyFilter();
            }

            var today = await _review.GetReviewedTodayAsync();
            if (today.Success)
            {
                ReviewedToday.Clear();
                foreach (var e in today.Data!) ReviewedToday.Add(e);
            }

            var all = await _review.GetAllSignedOffAsync();
            if (all.Success)
            {
                AllSignedOff.Clear();
                foreach (var e in all.Data!) AllSignedOff.Add(e);
            }
        });
    }

    [RelayCommand]
    private async Task OpenEncounterAsync(Encounter encounter)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.EncounterReview}?encounterId={encounter.Id}");
}

// ══════════════════════════════════════════════════════════════
//  3. ENCOUNTER REVIEW
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class EncounterReviewViewModel : BaseViewModel
{
    private readonly IReviewService _review;
    private readonly IAuthService   _auth;

    [ObservableProperty] private string     _encounterId = string.Empty;
    [ObservableProperty] private Encounter? _encounter;

    // Revision panel
    [ObservableProperty] private bool   _showRevisionPanel = false;
    [ObservableProperty] private string _revisionFeedback  = string.Empty;

    public EncounterReviewViewModel(IReviewService review, IAuthService auth)
    {
        _review = review;
        _auth   = auth;
        Title   = "Review Encounter";
    }

    partial void OnEncounterIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(EncounterId)) return;
        await RunBusyAsync(async () =>
        {
            var result = await _review.GetEncounterForReviewAsync(Guid.Parse(EncounterId));
            if (result.Success) Encounter = result.Data;
            else SetError(result.Error!);
        });
    }

    [RelayCommand]
    private void OpenRevisionPanel() => ShowRevisionPanel = true;

    [RelayCommand]
    private void HideRevisionPanel()
    {
        ShowRevisionPanel = false;
        RevisionFeedback  = string.Empty;
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (Encounter is null) return;

        bool confirmed = await Shell.Current.DisplayAlert(
            "Approve & Sign Off",
            "This encounter will be permanently locked. No further edits will be possible. Continue?",
            "Approve & Lock", "Cancel");

        if (!confirmed) return;

        await RunBusyAsync(async () =>
        {
            var opResult = await _review.GetCurrentSupervisorOpNumberAsync();
            if (!opResult.Success) { SetError(opResult.Error!); return; }
            var opNumber = opResult.Data!;

            var result = await _review.ApproveEncounterAsync(new ApproveEncounterRequest
            {
                EncounterId        = Guid.Parse(EncounterId),
                SupervisorOpNumber = opNumber,
            });

            if (!result.Success) { SetError(result.Error!); return; }

            await Shell.Current.GoToAsync(
                $"{AppRoutes.ApprovalSuccess}?encounterId={EncounterId}");
        });
    }

    [RelayCommand]
    private async Task SendRevisionAsync()
    {
        if (string.IsNullOrWhiteSpace(RevisionFeedback))
        { SetError("Please enter feedback for the student."); return; }

        await RunBusyAsync(async () =>
        {
            var result = await _review.RequestRevisionAsync(new RevisionRequest
            {
                EncounterId = Guid.Parse(EncounterId),
                Feedback    = RevisionFeedback.Trim(),
            });

            if (!result.Success) { SetError(result.Error!); return; }

            // Return to review queue
            await Shell.Current.GoToAsync($"//{AppRoutes.ReviewQueue}");
        });
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  4. APPROVAL SUCCESS
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class ApprovalSuccessViewModel : BaseViewModel
{
    [ObservableProperty] private string _encounterId = string.Empty;

    public ApprovalSuccessViewModel() => Title = "Encounter Approved";

    [RelayCommand]
    private async Task BackToQueueAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.ReviewQueue}");

    [RelayCommand]
    private async Task ViewSignedOffAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.SignedOffCases}");
}

// ══════════════════════════════════════════════════════════════
//  5. SIGNED-OFF CASES
// ══════════════════════════════════════════════════════════════
public partial class SignedOffCasesViewModel : BaseViewModel
{
    private readonly IReviewService _review;

    public ObservableCollection<Encounter> AllCases      { get; } = new();
    public ObservableCollection<Encounter> WeekCases     { get; } = new();
    public ObservableCollection<Encounter> MonthCases    { get; } = new();

    public SignedOffCasesViewModel(IReviewService review)
    {
        _review = review;
        Title   = "Signed-Off Cases";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _review.GetAllSignedOffAsync();
            if (!result.Success) { SetError(result.Error!); return; }

            var all  = result.Data!;
            var week  = DateTime.Today.AddDays(-7);
            var month = DateTime.Today.AddDays(-30);

            AllCases.Clear();
            WeekCases.Clear();
            MonthCases.Clear();

            foreach (var e in all)
            {
                AllCases.Add(e);
                if (e.SignedOffAt >= week)  WeekCases.Add(e);
                if (e.SignedOffAt >= month) MonthCases.Add(e);
            }
        });
    }

    [RelayCommand]
    private async Task ViewCaseAsync(Encounter encounter)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.SignedOffDetail}?encounterId={encounter.Id}");
}

// ══════════════════════════════════════════════════════════════
//  6. SIGNED-OFF DETAIL
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class SignedOffDetailViewModel : BaseViewModel
{
    private readonly IReviewService _review;

    [ObservableProperty] private string     _encounterId = string.Empty;
    [ObservableProperty] private Encounter? _encounter;
    [ObservableProperty] private bool       _isDownloading;

    public SignedOffDetailViewModel(IReviewService review)
    {
        _review = review;
        Title   = "Signed-Off Encounter";
    }

    partial void OnEncounterIdChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(EncounterId)) return;
        await RunBusyAsync(async () =>
        {
            // Signed-off encounters come from GetAllSignedOffAsync
            // GetEncounterForReviewAsync also works here
            var result = await _review.GetEncounterForReviewAsync(Guid.Parse(EncounterId));
            if (result.Success) Encounter = result.Data;
            else SetError(result.Error!);
        });
    }

    [RelayCommand]
    private async Task DownloadPdfAsync()
    {
        IsDownloading = true;
        try
        {
            var result = await _review.GetPdfUrlAsync(Guid.Parse(EncounterId));
            if (!result.Success) { SetError(result.Error!); return; }
            await Launcher.OpenAsync(result.Data!);
        }
        finally { IsDownloading = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  7. NOTIFICATIONS
// ══════════════════════════════════════════════════════════════
public partial class SupNotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _notif;

    public ObservableCollection<AppNotification> Notifications { get; } = new();
    [ObservableProperty] private int _unreadCount;

    public SupNotificationsViewModel(INotificationService notif)
    {
        _notif = notif;
        Title  = "Notifications";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _notif.GetMyNotificationsAsync();
            if (!result.Success) { SetError(result.Error!); return; }
            Notifications.Clear();
            foreach (var n in result.Data!) Notifications.Add(n);
            UnreadCount = Notifications.Count(n => !n.IsRead);
        });
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await _notif.MarkAllReadAsync();
        foreach (var n in Notifications) n.IsRead = true;
        UnreadCount = 0;
    }
}

// ══════════════════════════════════════════════════════════════
//  8. PROFILE
// ══════════════════════════════════════════════════════════════
public partial class SupProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profile;
    private readonly IAuthService    _auth;

    [ObservableProperty] private string _fullName  = string.Empty;
    [ObservableProperty] private string _email     = string.Empty;
    [ObservableProperty] private string _phone     = string.Empty;
    [ObservableProperty] private string _opNumber  = string.Empty;
    [ObservableProperty] private string _initials  = "?";
    [ObservableProperty] private bool   _saved;
    [ObservableProperty] private bool   _passwordChanged;

    // Exposed so XAML can bind {Binding UserProfile.Initials} etc.
    [ObservableProperty] private UserProfile? _userProfile;

    // Change password
    [ObservableProperty] private string _newPassword     = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool   _isPwVisible;

    public SupProfileViewModel(IProfileService profile, IAuthService auth)
    {
        _profile = profile;
        _auth    = auth;
        Title    = "My Profile";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _profile.GetProfileAsync();
            if (!result.Success) { SetError(result.Error!); return; }
            UserProfile = result.Data;
            FullName = result.Data!.FullName;
            Email    = result.Data.Email;
            Phone    = result.Data.Phone;
            Initials = result.Data.Initials;
        });
    }

    // Alias so both SaveCommand and SaveProfileCommand bindings work
    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName)) { SetError("Full name is required."); return; }
        await RunBusyAsync(async () =>
        {
            var result = await _profile.UpdateProfileAsync(FullName.Trim(), Phone.Trim());
            if (result.Success) Saved = true;
            else SetError(result.Error!);
        });
    }

    [RelayCommand]
    private void TogglePwVisibility() => IsPwVisible = !IsPwVisible;

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (NewPassword.Length < 8) { SetError("Password must be at least 8 characters."); return; }
        if (NewPassword != ConfirmPassword) { SetError("Passwords do not match."); return; }
        await RunBusyAsync(async () =>
        {
            var result = await _auth.ChangePasswordAsync(NewPassword);
            if (result.Success) { NewPassword = string.Empty; ConfirmPassword = string.Empty; Saved = true; PasswordChanged = true; }
            else SetError(result.Error!);
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirmed = await Shell.Current.DisplayAlert(
            "Log Out", "Are you sure you want to log out?", "Log Out", "Cancel");
        if (!confirmed) return;
        await _auth.SignOutAsync();
        App.RouteToLogin();
    }
}
