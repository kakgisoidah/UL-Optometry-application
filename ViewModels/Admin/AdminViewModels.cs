// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Admin/AdminViewModels.cs
//  All 18 Admin portal ViewModels.
// ════════════════════════════════════════════════════════════════════════

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Notification;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;
using UL_Optometry.ViewModels.Patient;

namespace UL_Optometry.ViewModels.Admin;
// ══════════════════════════════════════════════════════════════
//  1. AdminDashboardViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminDashboardViewModel : BaseViewModel
{
    private readonly IAdminBookingService _bookings;
    private readonly IAdminEncounterService _encounters;
    private readonly INotificationService _notifications;
    private readonly IAuditService _audit;

    [ObservableProperty] private int _bookingsTodayCount;
    [ObservableProperty] private int _pendingAssignmentCount;
    [ObservableProperty] private int _encountersTodayCount;
    [ObservableProperty] private int _unreadCount;
    [ObservableProperty] private ObservableCollection<AuditLog> _recentActivity = new();

    public AdminDashboardViewModel(
        IAdminBookingService bookings, IAdminEncounterService encounters,
        INotificationService notifications, IAuditService audit)
    {
        _bookings = bookings;
        _encounters = encounters;
        _notifications = notifications;
        _audit = audit;
        Title = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var br = await _bookings.GetAllBookingsAsync();
            if (br.Success)
            {
                BookingsTodayCount = br.Data?.Count(b => b.Date.Date == DateTime.Today) ?? 0;
                PendingAssignmentCount = br.Data?.Count(b => b.BookingStatus == BookingStatus.Pending) ?? 0;
            }

            var er = await _encounters.GetAllEncountersAsync();
            if (er.Success)
                EncountersTodayCount = er.Data?.Count(e => e.CreatedAt.Date == DateTime.Today) ?? 0;

            var nr = await _notifications.GetMyNotificationsAsync();
            if (nr.Success) UnreadCount = nr.Data?.Count(n => !n.IsRead) ?? 0;

            var ar = await _audit.GetLogsAsync(limit: 8);
            if (ar.Success)
                RecentActivity = new(ar.Data ?? new());
        });
    }

    [RelayCommand]
    private async Task GoToBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.AdminBookings}");
    [RelayCommand]
    private async Task GoToSchedulingAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.Scheduling}");
    [RelayCommand]
    private async Task GoToUsersAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.Users}");
    [RelayCommand]
    private async Task GoToEncountersAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.AdminEncounters}");
    [RelayCommand]
    private async Task GoToNotificationsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.AdminNotifications}");
    [RelayCommand]
    private async Task GoToAuditAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.Audit}");

    [RelayCommand]
    private async Task AuditRowTappedAsync(AuditLog log)
    {
        var route = log.Action.Contains("Booking")
            ? AppRoutes.AdminBookings
            : log.Action.Contains("Encounter") || log.Action.Contains("Approv")
                ? AppRoutes.AdminEncounters
                : log.Action.Contains("User")
                    ? AppRoutes.Users
                    : AppRoutes.Audit;
        await Shell.Current.GoToAsync($"//{route}");
    }
}

// ══════════════════════════════════════════════════════════════
//  2. SchedulingViewModel — helper view-items
// ══════════════════════════════════════════════════════════════

/// <summary>One cubicle chip shown in the 4-column cubicle grid.</summary>
public class CubicleViewItem
{
    public string CubicleName       { get; init; } = string.Empty;
    public bool   HasSupervisor     { get; init; }
    public string SupervisorName    { get; init; } = string.Empty;
    public string StudentName       { get; init; } = string.Empty;
    public bool   HasStudent        => !string.IsNullOrWhiteSpace(StudentName);
    public string SupervisorBadgeText  => HasSupervisor ? "Supervised" : "No Supervisor";
    public string SupervisorBadgeStyle => HasSupervisor ? "BadgeSuccessStyle" : "BadgeWarningStyle";
    public string StudentBadgeText     => HasStudent ? StudentName : "No Student";
    public string StudentBadgeStyle    => HasStudent ? "BadgeInfoStyle" : "BadgeMutedStyle";
}

/// <summary>One row in the Student Assignments table.</summary>
public partial class StudentAssignmentRow : ObservableObject
{
    public int    CubicleId    { get; init; }
    public string CubicleName  { get; init; } = string.Empty;
    public Guid?  StudentId    { get; init; }
    public string StudentName  { get; init; } = string.Empty;

    public Guid? SupervisorId { get; init; }
    public string SupervisorName { get; init; } = string.Empty;

    public bool HasStudent => StudentId.HasValue && !string.IsNullOrWhiteSpace(StudentName);
    public bool HasSupervisor => SupervisorId.HasValue && !string.IsNullOrWhiteSpace(SupervisorName);
    public string StudentInitial => HasStudent ? StudentName[0].ToString().ToUpper() : string.Empty;
    public string SupervisorInitial => HasSupervisor ? SupervisorName[0].ToString().ToUpper() : string.Empty;
    public string StatusText => HasStudent ? "Assigned" : "Empty";
    public string StatusBadgeStyle => HasStudent ? "BadgeSuccessStyle" : "BadgeMutedStyle";

    // Green dot = both assigned, amber = partial, grey = neither
    public string StatusDotStyle => (HasStudent && HasSupervisor) ? "DotSuccessStyle"
                                  : (HasStudent || HasSupervisor) ? "DotWarningStyle"
                                  : "DotMutedStyle";

}

/// <summary>One row in the Supervisor Coverage table.</summary>
public class SupervisorCoverageRow
{
    public int          CubicleId      { get; init; }
    public string       CubicleName    { get; init; } = string.Empty;
    public Guid?        SupervisorId   { get; init; }
    public string       SupervisorName { get; init; } = string.Empty;
    public List<string> OtherCubicles  { get; init; } = new();

    public bool   HasSupervisor     => SupervisorId.HasValue && !string.IsNullOrWhiteSpace(SupervisorName);
    public bool   HasOtherCubicles  => OtherCubicles.Count > 0;
    public string SupervisorInitial => HasSupervisor ? SupervisorName[0].ToString().ToUpper() : string.Empty;
    public string OnlyCubicleText   => $"Only {CubicleName}";
}

// ══════════════════════════════════════════════════════════════
//  2. SchedulingViewModel
// ══════════════════════════════════════════════════════════════
public partial class SchedulingViewModel : BaseViewModel
{
    private readonly ISchedulingService _scheduling;
    private readonly IUserService _users;

    // ── Core data ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Session>            _sessions          = new();
    [ObservableProperty] private ObservableCollection<Cubicle>            _cubicles          = new();
    [ObservableProperty] private ObservableCollection<SupervisorProfile>  _supervisors       = new();

    // ── Derived display collections ────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CubicleViewItem>     _cubicleViewItems  = new();
    [ObservableProperty] private ObservableCollection<StudentAssignmentRow> _studentAssignments = new();
    [ObservableProperty] private ObservableCollection<SupervisorCoverageRow>_supervisorRows    = new();

    // ── Selected session ───────────────────────────────────────────────
    [ObservableProperty] private Session? _selectedSession;
    [ObservableProperty] private bool     _maxSessionsReached;

    // ── Session gate: cubicle/student assigns locked when no sessions exist ──
    [ObservableProperty] private bool _cubiclesAreLocked = true;

    // ── Computed titles ────────────────────────────────────────────────
    public string SessionsCardTitle       => $"Sessions — {DateTime.Today:dd MMM yyyy}";
    public string StudentAssignTitle      => $"Assign Students — {SelectedSession?.TabLabel ?? "—"}";
    public string SupervisorCoverageTitle => $"Supervisor Coverage — {SelectedSession?.TabLabel ?? "—"}";

    // ── Supervisor / student picker items ──────────────────────────────
    [ObservableProperty] private ObservableCollection<string> _supervisorPickerItems = new();
    [ObservableProperty] private ObservableCollection<string> _studentPickerItems    = new();

    // ── ADD SESSION modal ──────────────────────────────────────────────
    [ObservableProperty] private bool     _showAddSession;
    [ObservableProperty] private string   _newSessionName     = string.Empty;
    [ObservableProperty] private TimeSpan _newStartTimePicker = new(8, 0, 0);
    [ObservableProperty] private TimeSpan _newEndTimePicker   = new(10, 0, 0);

    // ── EDIT SESSION modal ─────────────────────────────────────────────
    [ObservableProperty] private bool     _showEditSession;
    [ObservableProperty] private Session? _sessionBeingEdited;
    [ObservableProperty] private string   _editSessionName     = string.Empty;
    [ObservableProperty] private TimeSpan _editStartTimePicker = new(8, 0, 0);
    [ObservableProperty] private TimeSpan _editEndTimePicker   = new(10, 0, 0);
    public string EditSessionTitle => $"Edit Session — {SessionBeingEdited?.Name}";

    // ── ADD CUBICLE modal ──────────────────────────────────────────────
    [ObservableProperty] private bool   _showAddCubicle;
    [ObservableProperty] private string _newCubicleName       = string.Empty;
    [ObservableProperty] private string _newCubicleSupervisor = string.Empty;

    // ── ASSIGN STUDENT modal ───────────────────────────────────────────
    [ObservableProperty] private bool                  _showAssignStudent;
    [ObservableProperty] private StudentAssignmentRow? _assignStudentTarget;
    [ObservableProperty] private string                _selectedStudentForAssign = string.Empty;
    public string AssignStudentTitle       => $"Assign Student to {AssignStudentTarget?.CubicleName}";
    public bool   AssignStudentHasCurrent  => AssignStudentTarget?.HasStudent ?? false;
    public string AssignStudentCurrentName => AssignStudentTarget?.StudentName ?? string.Empty;

    // ── ASSIGN SUPERVISOR modal ────────────────────────────────────────
    [ObservableProperty] private bool                   _showAssignSupervisor;
    [ObservableProperty] private SupervisorCoverageRow? _assignSupervisorTarget;
    [ObservableProperty] private string                 _selectedSupervisorForAssign = string.Empty;
    public string AssignSupervisorTitle => $"Assign Supervisor to {AssignSupervisorTarget?.CubicleName}";

    // ── CUBICLE DETAIL modal ───────────────────────────────────────────
    [ObservableProperty] private bool               _showCubicleDetail;
    [ObservableProperty] private CubicleViewItem?   _cubicleDetailItem;
    [ObservableProperty] private SupervisorProfile? _cubicleDetailSupervisorProfile;
    public string CubicleDetailTitle           => $"Cubicle {CubicleDetailItem?.CubicleName}";
    public string CubicleDetailSupervisor      => CubicleDetailSupervisorProfile is not null
        ? $"{CubicleDetailSupervisorProfile.FullName} ({CubicleDetailSupervisorProfile.OpNumber})"
        : "Unassigned";
    public bool   CubicleDetailHasSupervisor   => CubicleDetailSupervisorProfile is not null;
    public string CubicleDetailAllCubiclesLabel => $"All cubicles supervised by {CubicleDetailSupervisorProfile?.FullName ?? "—"}";
    public ObservableCollection<string> CubicleDetailAllCubicles
        => new(CubicleDetailSupervisorProfile?.AssignedCubicleNames ?? new());
    public string CubicleDetailActionLabel
        => CubicleDetailHasSupervisor ? "Edit Supervisor's Cubicles" : "Assign Supervisor";

    // ══════════════════════════════════════════════════════════════════
    //  Constructor
    // ══════════════════════════════════════════════════════════════════
    public SchedulingViewModel(ISchedulingService scheduling, IUserService users)
    {
        _scheduling = scheduling;
        _users      = users;
        Title       = "Scheduling";
    }

    // ══════════════════════════════════════════════════════════════════
    //  LOAD
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var sr = await _scheduling.GetSessionsAsync();
            if (sr.Success)
            {
                Sessions = new(sr.Data ?? new());
                MaxSessionsReached = Sessions.Count >= 3;
                CubiclesAreLocked  = !Sessions.Any();
                if (SelectedSession is null && Sessions.Any())
                    SelectedSession = Sessions.First();
            }

            var cr = await _scheduling.GetCubiclesAsync();
            if (cr.Success) Cubicles = new(cr.Data ?? new());

            var supR = await _users.GetSupervisorsAsync();
            if (supR.Success)
            {
                Supervisors           = new(supR.Data ?? new());
                SupervisorPickerItems = new(Supervisors.Select(s =>
                    $"{s.FullName} (covers: {(s.AssignedCubicleNames.Any() ? string.Join(", ", s.AssignedCubicleNames) : "none")})"));
            }

            var stuR = await _users.GetStudentsAsync();
            if (stuR.Success)
                StudentPickerItems = new(stuR.Data?.Select(s => s.FullName) ?? Enumerable.Empty<string>());

            RebuildCubicleChips();
            await LoadAssignmentsAsync();
            RefreshTitles();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  SESSION TAB SELECTION
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task SelectSessionAsync(Session session)
    {
        SelectedSession = session;
        await LoadAssignmentsAsync();
        RefreshTitles();
    }

    private async Task LoadAssignmentsAsync()
    {
        if (SelectedSession is null) { StudentAssignments = new(); SupervisorRows = new(); RebuildCubicleChips(); return; }

        var ar = await _scheduling.GetAssignmentsAsync(SelectedSession.Id);
        if (!ar.Success) return;

        var assignments = ar.Data ?? new();

        StudentAssignments = new(Cubicles.Select(c =>
        {
            var a = assignments.FirstOrDefault(x => x.CubicleId == c.Id);
            return new StudentAssignmentRow
            {
                CubicleId   = c.Id,
                CubicleName = c.Name,
                StudentId   = a?.StudentId,
                StudentName = a?.StudentName ?? string.Empty,
            };
        }));

        SupervisorRows = new(Cubicles.Select(c =>
        {
            var sup    = Supervisors.FirstOrDefault(s => s.AssignedCubicleIds.Contains(c.Id));
            var others = sup?.AssignedCubicleNames.Where(n => n != c.Name).ToList() ?? new();
            return new SupervisorCoverageRow
            {
                CubicleId      = c.Id,
                CubicleName    = c.Name,
                SupervisorId   = sup?.UserId,
                SupervisorName = sup?.FullName ?? string.Empty,
                OtherCubicles  = others,
            };
        }));

        RebuildCubicleChips();
    }

    private void RebuildCubicleChips()
    {
        CubicleViewItems = new(Cubicles.Select(c =>
        {
            var hasSup     = Supervisors.Any(s => s.AssignedCubicleIds.Contains(c.Id));
            var supName    = Supervisors.FirstOrDefault(s => s.AssignedCubicleIds.Contains(c.Id))?.FullName ?? string.Empty;
            var assignment = StudentAssignments.FirstOrDefault(a => a.CubicleId == c.Id);
            return new CubicleViewItem
            {
                CubicleName    = c.Name,
                HasSupervisor  = hasSup,
                SupervisorName = supName,
                StudentName    = assignment?.StudentName ?? string.Empty,
            };
        }));
    }

    private void RefreshTitles()
    {
        OnPropertyChanged(nameof(StudentAssignTitle));
        OnPropertyChanged(nameof(SupervisorCoverageTitle));
        OnPropertyChanged(nameof(SessionsCardTitle));
    }

    // ══════════════════════════════════════════════════════════════════
    //  ADD SESSION
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenAddSession()
    {
        NewSessionName     = string.Empty;
        NewStartTimePicker = new TimeSpan(8, 0, 0);
        NewEndTimePicker   = new TimeSpan(10, 0, 0);
        ShowAddSession     = true;
    }

    [RelayCommand]
    private void CancelAddSession() => ShowAddSession = false;

    [RelayCommand]
    private async Task AddSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSessionName))
        { SetError("Session name is required."); return; }
        if (NewEndTimePicker <= NewStartTimePicker)
        { SetError("End time must be after start time."); return; }

        string start = $"{NewStartTimePicker.Hours:D2}:{NewStartTimePicker.Minutes:D2}";
        string end   = $"{NewEndTimePicker.Hours:D2}:{NewEndTimePicker.Minutes:D2}";

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.AddSessionAsync(NewSessionName.Trim(), start, end);
            if (!r.Success) { SetError(r.Error!); return; }
            ShowAddSession = false;
            await LoadAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  EDIT SESSION
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenEditSession(Session session)
    {
        SessionBeingEdited = session;
        EditSessionName    = session.Name;

        var parts = session.Display?.Split('–');
        if (parts?.Length >= 2 &&
            TimeSpan.TryParse(parts[0].Trim(), out var s) &&
            TimeSpan.TryParse(parts[1].Trim(), out var e))
        {
            EditStartTimePicker = s;
            EditEndTimePicker   = e;
        }

        ShowEditSession = true;
        OnPropertyChanged(nameof(EditSessionTitle));
    }

    [RelayCommand]
    private void CancelEditSession() { ShowEditSession = false; SessionBeingEdited = null; }

    [RelayCommand]
    private async Task SaveEditSessionAsync()
    {
        if (SessionBeingEdited is null || string.IsNullOrWhiteSpace(EditSessionName))
        { SetError("Session name is required."); return; }
        if (EditEndTimePicker <= EditStartTimePicker)
        { SetError("End time must be after start time."); return; }

        string start = $"{EditStartTimePicker.Hours:D2}:{EditStartTimePicker.Minutes:D2}";
        string end   = $"{EditEndTimePicker.Hours:D2}:{EditEndTimePicker.Minutes:D2}";

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.UpdateSessionAsync(
                SessionBeingEdited.Id, EditSessionName.Trim(), start, end);
            if (!r.Success) { SetError(r.Error!); return; }
            ShowEditSession    = false;
            SessionBeingEdited = null;
            await LoadAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  DELETE SESSION
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task DeleteSessionAsync(Session session)
    {
        bool ok = await Shell.Current.DisplayAlert(
            "Delete Session",
            $"Delete '{session.Name}'? All cubicle and student assignments for this session will be removed.",
            "Delete", "Cancel");
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.DeleteSessionAsync(session.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            if (SelectedSession?.Id == session.Id) SelectedSession = null;
            await LoadAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  ADD CUBICLE
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenAddCubicle()
    {
        NewCubicleName       = string.Empty;
        NewCubicleSupervisor = string.Empty;
        ShowAddCubicle       = true;
    }

    [RelayCommand]
    private void CancelAddCubicle() => ShowAddCubicle = false;

    [RelayCommand]
    private async Task AddCubicleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCubicleName))
        { SetError("Cubicle name is required."); return; }

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.AddCubicleAsync(NewCubicleName.Trim());
            if (!r.Success) { SetError(r.Error!); return; }

            if (!string.IsNullOrWhiteSpace(NewCubicleSupervisor) && r.Data is not null)
            {
                var sup = Supervisors.FirstOrDefault(s =>
                    NewCubicleSupervisor.StartsWith(s.FullName, StringComparison.OrdinalIgnoreCase));
                if (sup is not null)
                {
                    var ids = new List<int>(sup.AssignedCubicleIds) { r.Data.Id };
                    await _users.AssignSupervisorCubiclesAsync(sup.UserId, ids);
                }
            }

            NewCubicleName = string.Empty;
            ShowAddCubicle = false;
            await LoadAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  CUBICLE DETAIL
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenCubicleDetail(CubicleViewItem item)
    {
        CubicleDetailItem = item;
        CubicleDetailSupervisorProfile = Supervisors.FirstOrDefault(s =>
            s.AssignedCubicleNames.Contains(item.CubicleName));
        ShowCubicleDetail = true;
        OnPropertyChanged(nameof(CubicleDetailTitle));
        OnPropertyChanged(nameof(CubicleDetailSupervisor));
        OnPropertyChanged(nameof(CubicleDetailHasSupervisor));
        OnPropertyChanged(nameof(CubicleDetailAllCubiclesLabel));
        OnPropertyChanged(nameof(CubicleDetailAllCubicles));
        OnPropertyChanged(nameof(CubicleDetailActionLabel));
    }

    [RelayCommand]
    private void CloseCubicleDetail() { ShowCubicleDetail = false; CubicleDetailItem = null; }

    [RelayCommand]
    private void CubicleDetailAction()
    {
        if (CubicleDetailItem is null) return;
        ShowCubicleDetail = false;
        var row = SupervisorRows.FirstOrDefault(r => r.CubicleName == CubicleDetailItem.CubicleName);
        if (row is not null) OpenAssignSupervisor(row);
    }

    // ══════════════════════════════════════════════════════════════════
    //  ASSIGN STUDENT
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenAssignStudent(StudentAssignmentRow row)
    {
        AssignStudentTarget      = row;
        SelectedStudentForAssign = row.StudentName;
        ShowAssignStudent        = true;
        OnPropertyChanged(nameof(AssignStudentTitle));
        OnPropertyChanged(nameof(AssignStudentHasCurrent));
        OnPropertyChanged(nameof(AssignStudentCurrentName));
    }

    /// <summary>Open the assign-student modal directly from a cubicle chip.</summary>
    [RelayCommand]
    private void OpenAssignStudentFromChip(CubicleViewItem chip)
    {
        var row = StudentAssignments.FirstOrDefault(r => r.CubicleName == chip.CubicleName);
        if (row is null) return;
        OpenAssignStudent(row);
    }

    [RelayCommand]
    private void CancelAssignStudent() { ShowAssignStudent = false; AssignStudentTarget = null; }

    [RelayCommand]
    private async Task ConfirmAssignStudentAsync()
    {
        if (AssignStudentTarget is null || SelectedSession is null) return;
        if (CubiclesAreLocked)
        { SetError("Create at least one session before assigning students."); return; }
        if (string.IsNullOrWhiteSpace(SelectedStudentForAssign))
        { SetError("Select a student."); return; }

        var student = await ResolveStudentByNameAsync(SelectedStudentForAssign);
        if (student is null) { SetError("Student not found."); return; }

        // Enforce 1 cubicle per student per session
        var conflict = StudentAssignments.FirstOrDefault(r =>
            r.StudentId == student.UserId &&
            r.CubicleId != AssignStudentTarget.CubicleId);
        if (conflict is not null)
        {
            SetError($"{student.FullName} is already assigned to {conflict.CubicleName} in this session. Each student may only hold one cubicle per session.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.AssignStudentAsync(
                AssignStudentTarget.CubicleId, SelectedSession.Id, student.UserId);
            if (!r.Success) { SetError(r.Error!); return; }
            ShowAssignStudent   = false;
            AssignStudentTarget = null;
            await LoadAssignmentsAsync();
        });
    }

    [RelayCommand]
    private async Task RemoveStudentAsync(StudentAssignmentRow row)
    {
        if (SelectedSession is null) return;
        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.RemoveStudentAsync(row.CubicleId, SelectedSession.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            await LoadAssignmentsAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  ASSIGN SUPERVISOR
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void OpenAssignSupervisorForRow(StudentAssignmentRow row)
    {
        // Build a temporary SupervisorCoverageRow from the unified card row
        var supRow = new SupervisorCoverageRow
        {
            CubicleId = row.CubicleId,
            CubicleName = row.CubicleName,
            SupervisorName = row.SupervisorName ?? string.Empty,
        };
        OpenAssignSupervisor(supRow);
    }

   

    [RelayCommand]
    private void OpenAssignSupervisor(SupervisorCoverageRow row)
    {
        AssignSupervisorTarget = row;
        SelectedSupervisorForAssign = row.HasSupervisor
            ? SupervisorPickerItems.FirstOrDefault(p =>
                p.StartsWith(row.SupervisorName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty
            : string.Empty;
        ShowAssignSupervisor = true;
        OnPropertyChanged(nameof(AssignSupervisorTitle));
    }

    [RelayCommand]
    private void CancelAssignSupervisor()
    { ShowAssignSupervisor = false; AssignSupervisorTarget = null; }

    [RelayCommand]
    private async Task RemoveSupervisorAsync(StudentAssignmentRow row)
    {
        await RunBusyAsync(async () =>
        {
            await _scheduling.RemoveSupervisorAsync(row.CubicleId, DateTime.Today);
            await LoadAssignmentsAsync();
        });
    }

    [RelayCommand]
    private async Task ConfirmAssignSupervisorAsync()
    {
        if (AssignSupervisorTarget is null || string.IsNullOrWhiteSpace(SelectedSupervisorForAssign))
        { SetError("Select a supervisor."); return; }

        var sup = Supervisors.FirstOrDefault(s =>
            SelectedSupervisorForAssign.StartsWith(s.FullName, StringComparison.OrdinalIgnoreCase));
        if (sup is null) { SetError("Supervisor not found."); return; }

        await RunBusyAsync(async () =>
        {
            var ids = new List<int>(sup.AssignedCubicleIds);
            if (!ids.Contains(AssignSupervisorTarget.CubicleId))
                ids.Add(AssignSupervisorTarget.CubicleId);

            var r = await _users.AssignSupervisorCubiclesAsync(sup.UserId, ids);
            if (!r.Success) { SetError(r.Error!); return; }
            ShowAssignSupervisor   = false;
            AssignSupervisorTarget = null;
            await LoadAsync();
        });
    }

    // ══════════════════════════════════════════════════════════════════
    //  NAVIGATE
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task GoToSupervisorsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.AdminSupervisors}");

    // ══════════════════════════════════════════════════════════════════
    //  Private helpers
    // ══════════════════════════════════════════════════════════════════
    private async Task<StudentProfile?> ResolveStudentByNameAsync(string name)
    {
        var r = await _users.GetStudentsAsync();
        return r.Success
            ? r.Data?.FirstOrDefault(s => s.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
            : null;
    }
}

// ══════════════════════════════════════════════════════════════
//  3. AdminBookingsViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminBookingsViewModel : BaseViewModel
{
    private readonly IAdminBookingService _bookings;

    [ObservableProperty] private ObservableCollection<Booking> _all = new();
    [ObservableProperty] private ObservableCollection<Booking> _pending = new();
    [ObservableProperty] private ObservableCollection<Booking> _accepted = new();
    [ObservableProperty] private ObservableCollection<Booking> _completed = new();
    [ObservableProperty] private ObservableCollection<Booking> _cancelled = new();
    [ObservableProperty] private bool _allIsEmpty, _pendingIsEmpty;
    [ObservableProperty] private int _autoAssignedCount;
    [ObservableProperty] private bool _autoAssignDone;

    public AdminBookingsViewModel(IAdminBookingService bookings)
    { _bookings = bookings; Title = "Bookings"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.GetAllBookingsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            var data = r.Data ?? new();
            All = new(data);
            Pending = new(data.Where(b => b.BookingStatus == BookingStatus.Pending));
            Accepted = new(data.Where(b => b.BookingStatus == BookingStatus.Accepted ||
                                             b.BookingStatus == BookingStatus.InProgress));
            Completed = new(data.Where(b => b.BookingStatus == BookingStatus.Completed));
            Cancelled = new(data.Where(b => b.BookingStatus == BookingStatus.Cancelled));
            AllIsEmpty = !All.Any();
            PendingIsEmpty = !Pending.Any();
        });
    }

    [RelayCommand]
    private async Task ViewBookingAsync(Booking b)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminBookingDetail}?bookingId={b.Id}");

    [RelayCommand]
    private async Task AutoAssignAllAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.AutoAssignAllPendingAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            AutoAssignedCount = r.Data;
            AutoAssignDone = true;
            await LoadAsync();
        });
    }
}

// ══════════════════════════════════════════════════════════════
//  4. AdminBookingDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class AdminBookingDetailViewModel : BaseViewModel
{
    private readonly IAdminBookingService _bookings;
    private readonly IUserService _users;
    private readonly INotificationService _notifications;

    [ObservableProperty] private string _bookingId = string.Empty;
    [ObservableProperty] private Booking? _booking;
    [ObservableProperty] private ObservableCollection<SupervisorProfile> _supervisors = new();
    [ObservableProperty] private SupervisorProfile? _selectedSupervisor;
    [ObservableProperty] private bool _assigned;

    public AdminBookingDetailViewModel(
        IAdminBookingService bookings, IUserService users, INotificationService notifications)
    { _bookings = bookings; _users = users; _notifications = notifications; Title = "Booking Detail"; }

    // Fires every time the page appears — resets and reloads fresh
    [RelayCommand]
    private async Task LoadAsync()
    {
        Assigned = false;
        Booking = null;
        Supervisors = new();
        SelectedSupervisor = null;
        ClearError();

        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(BookingId, out var id))
            {
                SetError("Could not load booking.");
                return;
            }

            var br = await _bookings.GetBookingByIdAsync(id);
            if (!br.Success) { SetError(br.Error!); return; }
            Booking = br.Data;
            OnPropertyChanged(nameof(Booking));

            var sr = await _users.GetSupervisorsAsync();
            if (sr.Success)
            {
                Supervisors = new((sr.Data ?? new())
                    .Where(s => !string.IsNullOrWhiteSpace(s.FullName)));
                OnPropertyChanged(nameof(Supervisors));
            }
        });
    }

    [RelayCommand]
    private async Task AssignAsync()
    {
        if (SelectedSupervisor is null)
        { SetError("Please select a supervisor."); return; }
        if (!Guid.TryParse(BookingId, out var id)) return;

        await RunBusyAsync(async () =>
        {
            var r = await _bookings.AssignBookingAsync(new AssignBookingRequest
            {
                BookingId = id,
                SupervisorId = SelectedSupervisor.UserId,
                StudentId = Guid.Empty,
                CubicleId = 0,
            });
            if (!r.Success) { SetError(r.Error!); return; }
            Assigned = true;
        });
    }

    [RelayCommand]
    private async Task CancelBookingAsync()
    {
        bool ok = await Shell.Current.DisplayAlert("Cancel Booking",
            "Are you sure you want to cancel this booking?", "Cancel Booking", "Keep");
        if (!ok) return;
        if (!Guid.TryParse(BookingId, out var id)) return;

        await RunBusyAsync(async () =>
        {
            var r = await _bookings.CancelBookingAsync(id, "Cancelled by admin");
            if (!r.Success) { SetError(r.Error!); return; }
            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task NotifyPatientAsync()
    {
        if (Booking is null) return;
        await _notifications.SendToUserAsync(Booking.PatientId,
            "Booking Update", "Your booking has been updated by the admin.");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  5. UsersViewModel
// ══════════════════════════════════════════════════════════════
public partial class UsersViewModel : BaseViewModel
{
    private readonly IUserService _users;

    [ObservableProperty] private ObservableCollection<UserProfile> _allUsers = new();
    [ObservableProperty] private ObservableCollection<UserProfile> _filtered = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _roleFilter = "All";
    [ObservableProperty] private bool _isEmpty;

    public List<string> RoleFilters { get; } = new()
    { "All", "admin", "supervisor", "student", "patient" };

    public UsersViewModel(IUserService users) { _users = users; Title = "Users"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _users.GetAllUsersAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            AllUsers = new(r.Data ?? new());
            ApplyFilter();
        });
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnRoleFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = AllUsers.AsEnumerable();
        if (RoleFilter != "All")
            q = q.Where(u => u.RoleString.Equals(RoleFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(u => u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                              u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        Filtered = new(q);
        IsEmpty = !Filtered.Any();
    }

    [RelayCommand]
    private async Task ViewUserAsync(UserProfile u)
        => await Shell.Current.GoToAsync($"{AppRoutes.UserDetail}?userId={u.UserId}");

    [RelayCommand]
    private async Task AddUserAsync()
        => await Shell.Current.GoToAsync(AppRoutes.AddUser);

    [RelayCommand]
    private async Task DeactivateAsync(UserProfile u)
    {
        bool ok = await Shell.Current.DisplayAlert("Deactivate",
            $"Deactivate {u.FullName}?", "Deactivate", "Cancel");
        if (!ok) return;
        await RunBusyAsync(async () =>
        {
            var r = await _users.DeactivateUserAsync(u.UserId);
            if (!r.Success) { SetError(r.Error!); return; }
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(UserProfile u)
    {
        await RunBusyAsync(async () => await _users.ResetPasswordAsync(u.UserId));
    }
}

// ══════════════════════════════════════════════════════════════
//  6. AddUserViewModel
// ══════════════════════════════════════════════════════════════
public partial class AddUserViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly ISchedulingService _scheduling;

    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _selectedRole = string.Empty;
    [ObservableProperty] private string _generatedPassword = string.Empty;
    [ObservableProperty] private bool _created;

    // Supervisor fields
    [ObservableProperty] private string _opNumber = string.Empty;
    [ObservableProperty] private string _qualification = string.Empty;
    [ObservableProperty] private ObservableCollection<CubicleSelection> _cubicleSelections = new();

    // Student fields
    [ObservableProperty] private string _studentNumber = string.Empty;
    [ObservableProperty] private int _yearOfStudy = 3;

    // Patient fields
    [ObservableProperty] private string _idNumber = string.Empty;
    [ObservableProperty] private string _gender = string.Empty;
    [ObservableProperty] private DateTime _dob = DateTime.Today.AddYears(-25);

    public bool IsSupervisor => SelectedRole == Roles.Supervisor;
    public bool IsStudent => SelectedRole == Roles.Student;
    public bool IsPatient => SelectedRole == Roles.Patient;

    public List<string> RoleOptions { get; } = new()
    { Roles.Admin, Roles.Supervisor, Roles.Student, Roles.Patient };
    public List<int> YearOptions { get; } = new() { 1, 2, 3, 4, 5, 6 };
    public List<string> GenderOptions { get; } = new() { "Male", "Female", "Other" };

    public AddUserViewModel(IUserService users, ISchedulingService scheduling)
    {
        _users = users;
        _scheduling = scheduling;
        Title = "Add User";
        GeneratePassword();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var cr = await _scheduling.GetCubiclesAsync();
            if (cr.Success)
            {
                CubicleSelections = new(cr.Data!.Select(c => new CubicleSelection
                { CubicleId = c.Id, CubicleName = c.Name }));
            }
        });
    }

    partial void OnSelectedRoleChanged(string value)
    {
        OnPropertyChanged(nameof(IsSupervisor));
        OnPropertyChanged(nameof(IsStudent));
        OnPropertyChanged(nameof(IsPatient));
    }

    [RelayCommand]
    private void GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        // Use a cryptographically secure RNG — System.Random is not suitable for passwords
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(12);
        GeneratedPassword = new string(
            bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName)) { SetError("Full name is required."); return; }
        if (string.IsNullOrWhiteSpace(Email)) { SetError("Email is required."); return; }
        if (string.IsNullOrWhiteSpace(SelectedRole)) { SetError("Select a role."); return; }

        await RunBusyAsync(async () =>
        {
            var r = await _users.CreateUserAsync(new CreateUserRequest
            {
                FullName = FullName.Trim(),
                Email = Email.Trim().ToLowerInvariant(),
                DefaultPassword = GeneratedPassword,
                Phone = Phone.Trim(),
                Role = UserRoleExtensions.FromDbString(SelectedRole),
                OpNumber = OpNumber.Trim(),
                Qualification = Qualification.Trim(),
                CubicleIds = CubicleSelections.Where(c => c.IsSelected).Select(c => c.CubicleId).ToList(),
                StudentNumber = StudentNumber.Trim(),
                YearOfStudy = YearOfStudy,
                IdNumber = IdNumber.Trim(),
                DateOfBirth = Dob,
                Gender = Gender,
            });

            if (!r.Success) { SetError(r.Error!); return; }
            Created = true;
        });
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

/// <summary>Cubicle checkbox item for supervisor assignment.</summary>
public partial class CubicleSelection : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public int CubicleId { get; init; }
    public string CubicleName { get; init; } = string.Empty;
}

// ══════════════════════════════════════════════════════════════
//  7. UserDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(UserId), "userId")]
public partial class UserDetailViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly INotificationService _notifications;

    [ObservableProperty] private string _userId = string.Empty;
    [ObservableProperty] private UserProfile? _user;
    [ObservableProperty] private bool _deactivated;
    [ObservableProperty] private bool _passwordReset;

    public UserDetailViewModel(IUserService users, INotificationService notifications)
    { _users = users; _notifications = notifications; Title = "User Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(UserId, out var id)) return;
            var r = await _users.GetUserByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            User = r.Data;
            Title = User?.FullName ?? "User Detail";
        });
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(UserId, out var id)) return;
            var r = await _users.ResetPasswordAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            PasswordReset = true;
        });
    }

    [RelayCommand]
    private async Task DeactivateAsync()
    {
        bool ok = await Shell.Current.DisplayAlert("Deactivate", "Deactivate this user?", "Deactivate", "Cancel");
        if (!ok) return;
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(UserId, out var id)) return;
            var r = await _users.DeactivateUserAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Deactivated = true;
        });
    }

    [RelayCommand]
    private async Task SendNotificationAsync()
    {
        if (User is null) return;
        await _notifications.SendToUserAsync(User.UserId,
            "Message from Admin", "Please check your latest updates.");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  8. AdminStudentsViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminStudentsViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly IPoeService _poe;

    [ObservableProperty] private ObservableCollection<StudentProfile> _all = new();
    [ObservableProperty] private ObservableCollection<StudentProfile> _atRisk = new();
    [ObservableProperty] private ObservableCollection<StudentProfile> _inactive = new();
    [ObservableProperty] private bool _isEmpty;

    public AdminStudentsViewModel(IUserService users, IPoeService poe)
    { _users = users; _poe = poe; Title = "Students"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _users.GetStudentsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            var data = r.Data ?? new();
            All = new(data);
            AtRisk = new(data.Where(s => s.PoePercent < 50));
            Inactive = new(data.Where(s => !s.IsActive));
            IsEmpty = !All.Any();
        });
    }

    [RelayCommand]
    private async Task ViewStudentAsync(StudentProfile s)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminStudentDetail}?userId={s.UserId}");

    [RelayCommand]
    private async Task AddStudentAsync()
        => await Shell.Current.GoToAsync(AppRoutes.AddUser);
}

// ══════════════════════════════════════════════════════════════
//  9. AdminStudentDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(UserId), "userId")]
public partial class AdminStudentDetailViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly IPoeService _poe;
    private readonly IAdminEncounterService _encounters;
    private readonly INotificationService _notifications;

    [ObservableProperty] private string _userId = string.Empty;
    [ObservableProperty] private StudentProfile? _student;
    [ObservableProperty] private PoeSummary _poeSummary = new();
    [ObservableProperty] private ObservableCollection<Encounter> _encounters_ = new();

    public AdminStudentDetailViewModel(
        IUserService users, IPoeService poe,
        IAdminEncounterService encounters, INotificationService notifications)
    { _users = users; _poe = poe; _encounters = encounters; _notifications = notifications; Title = "Student Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var students = await _users.GetStudentsAsync();
            if (students.Success && Guid.TryParse(UserId, out var uid))
            {
                Student = students.Data?.FirstOrDefault(s => s.UserId == uid);
                if (Student is not null) Title = Student.FullName;
            }

            if (Guid.TryParse(UserId, out var id))
            {
                var pr = await _poe.GetPoeSummaryForStudentAsync(id);
                if (pr.Success) PoeSummary = pr.Data ?? new();
            }

            var er = await _encounters.GetAllEncountersAsync();
            if (er.Success && Student is not null)
                Encounters_ = new(er.Data?.Where(e => e.StudentId == Student.UserId) ?? new List<Encounter>());
        });
    }

    [RelayCommand]
    private async Task SendNotificationAsync()
    {
        if (Student is null) return;
        await _notifications.SendToUserAsync(Student.UserId,
            "Message from Admin", "Please check your latest updates from the clinic.");
    }

    [RelayCommand]
    private async Task ViewEncounterAsync(Encounter e)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminEncounterDetail}?encounterId={e.Id}");

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  10. AdminSupervisorsViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminSupervisorsViewModel : BaseViewModel
{
    private readonly IUserService _users;

    [ObservableProperty] private ObservableCollection<SupervisorProfile> _supervisors = new();
    [ObservableProperty] private ObservableCollection<Cubicle> _cubicles = new();
    [ObservableProperty] private bool _isEmpty;

    public AdminSupervisorsViewModel(IUserService users)
    { _users = users; Title = "Supervisors"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _users.GetSupervisorsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            Supervisors = new(r.Data ?? new());
            IsEmpty = !Supervisors.Any();
        });
    }

    [RelayCommand]
    private async Task ViewSupervisorAsync(SupervisorProfile s)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminSupervisorDetail}?userId={s.UserId}");

    [RelayCommand]
    private async Task AddSupervisorAsync()
        => await Shell.Current.GoToAsync(AppRoutes.AddUser);

    [RelayCommand]
    private async Task AssignCubiclesAsync(SupervisorProfile s)
    {
        var ids = s.AssignedCubicleIds;
        var r = await _users.AssignSupervisorCubiclesAsync(s.UserId, ids);
        if (!r.Success) SetError(r.Error!);
    }
}

// ══════════════════════════════════════════════════════════════
//  11. AdminSupervisorDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(UserId), "userId")]
public partial class AdminSupervisorDetailViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly INotificationService _notifications;

    [ObservableProperty] private string _userId = string.Empty;
    [ObservableProperty] private SupervisorProfile? _supervisor;
    [ObservableProperty] private List<int> _selectedCubicleIds = new();
    [ObservableProperty] private bool _saved;

    public AdminSupervisorDetailViewModel(IUserService users, INotificationService notifications)
    { _users = users; _notifications = notifications; Title = "Supervisor Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var sups = await _users.GetSupervisorsAsync();
            if (sups.Success && Guid.TryParse(UserId, out var uid))
            {
                Supervisor = sups.Data?.FirstOrDefault(s => s.UserId == uid);
                if (Supervisor is not null)
                {
                    Title = Supervisor.FullName;
                    SelectedCubicleIds = new(Supervisor.AssignedCubicleIds);
                }
            }
        });
    }

    [RelayCommand]
    private async Task SaveCubiclesAsync()
    {
        if (Supervisor is null) return;
        await RunBusyAsync(async () =>
        {
            var r = await _users.AssignSupervisorCubiclesAsync(Supervisor.UserId, SelectedCubicleIds);
            if (!r.Success) { SetError(r.Error!); return; }
            Saved = true;
        });
    }

    [RelayCommand]
    private async Task SendNotificationAsync()
    {
        if (Supervisor is null) return;
        await _notifications.SendToUserAsync(Supervisor.UserId,
            "Message from Admin", "Please check your latest updates.");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  12. AdminEncountersViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminEncountersViewModel : BaseViewModel
{
    private readonly IAdminEncounterService _encounters;

    [ObservableProperty] private ObservableCollection<Encounter> _all = new();
    [ObservableProperty] private ObservableCollection<Encounter> _underReview = new();
    [ObservableProperty] private ObservableCollection<Encounter> _approved = new();
    [ObservableProperty] private ObservableCollection<Encounter> _revision = new();
    [ObservableProperty] private ObservableCollection<Encounter> _drafts = new();
    [ObservableProperty] private bool _isEmpty;

    public AdminEncountersViewModel(IAdminEncounterService encounters)
    { _encounters = encounters; Title = "Encounters"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _encounters.GetAllEncountersAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            var data = r.Data ?? new();
            All = new(data);
            UnderReview = new(data.Where(e => e.EncounterStatus is
                EncounterStatus.Submitted or EncounterStatus.UnderReview));
            Approved = new(data.Where(e => e.EncounterStatus == EncounterStatus.Approved));
            Revision = new(data.Where(e => e.EncounterStatus == EncounterStatus.Revision));
            Drafts = new(data.Where(e => e.EncounterStatus == EncounterStatus.Draft));
            IsEmpty = !All.Any();
        });
    }

    [RelayCommand]
    private async Task ViewEncounterAsync(Encounter e)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminEncounterDetail}?encounterId={e.Id}");
}

// ══════════════════════════════════════════════════════════════
//  13. AdminEncounterDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class AdminEncounterDetailViewModel : BaseViewModel
{
    private readonly IAdminEncounterService _encounters;

    [ObservableProperty] private string _encounterId = string.Empty;
    [ObservableProperty] private Encounter? _encounter;
    [ObservableProperty] private string? _pdfUrl;

    public AdminEncounterDetailViewModel(IAdminEncounterService encounters)
    { _encounters = encounters; Title = "Encounter Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(EncounterId, out var id)) return;
            var r = await _encounters.GetEncounterByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Encounter = r.Data;
            Title = $"{Encounter?.StudentName} — {Encounter?.DateDisplay}";
        });
    }

    [RelayCommand]
    private async Task DownloadPdfAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(EncounterId, out var id)) return;
            var r = await _encounters.GetPdfUrlAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            PdfUrl = r.Data;
            await Launcher.OpenAsync(PdfUrl!);
        });
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  14. AdminPoeViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminPoeViewModel : BaseViewModel
{
    private readonly IPoeService _poe;
    private readonly IUserService _users;
    private readonly INotificationService _notifications;

    [ObservableProperty] private double _averagePct;
    [ObservableProperty] private int _onTrackCount;
    [ObservableProperty] private int _atRiskCount;
    [ObservableProperty] private ObservableCollection<(Guid UserId, string Name, double Pct)> _studentPoe = new();

    public AdminPoeViewModel(IPoeService poe, IUserService users, INotificationService notifications)
    { _poe = poe; _users = users; _notifications = notifications; Title = "PoE Overview"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var avgR = await _poe.GetAveragePoePercentAsync();
            if (avgR.Success) AveragePct = avgR.Data;

            var allR = await _poe.GetAllStudentPoeAsync();
            if (allR.Success)
            {
                var data = allR.Data ?? new();
                StudentPoe = new(data);
                OnTrackCount = data.Count(x => x.PoePercent >= 70);
                AtRiskCount = data.Count(x => x.PoePercent < 50);
            }
        });
    }

    [RelayCommand]
    private async Task ViewStudentAsync(Guid userId)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.AdminStudentDetail}?userId={userId}");

    [RelayCommand]
    private async Task NotifyAllStudentsAsync()
        => await _notifications.SendToRoleAsync(Roles.Student,
            "PoE Reminder", "Please ensure you are logging your clinical hours.");

    [RelayCommand]
    private async Task GoToReportsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.Reports}");
}

// ══════════════════════════════════════════════════════════════
//  15. AdminNotificationsViewModel
// ══════════════════════════════════════════════════════════════
public partial class AdminNotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _svc;
    private readonly IUserService _users;

    // Compose form
    [ObservableProperty] private string _recipientType = "all";  // all|students|supervisors|patients|user
    [ObservableProperty] private string _recipientEmail = string.Empty;
    [ObservableProperty] private string _notifTitle = string.Empty;
    [ObservableProperty] private string _notifMessage = string.Empty;
    [ObservableProperty] private bool _isIndividual;
    [ObservableProperty] private bool _sent;

    // History / inbox
    [ObservableProperty] private ObservableCollection<AppNotification> _sent_ = new();
    [ObservableProperty] private ObservableCollection<AppNotification> _inbox = new();
    [ObservableProperty] private ObservableCollection<UserProfile> _allUsers = new();

    public List<string> RecipientTypes { get; } = new()
    { "All Users", "All Students", "All Supervisors", "All Patients", "Specific User" };

    public AdminNotificationsViewModel(INotificationService svc, IUserService users)
    { _svc = svc; _users = users; Title = "Notifications"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var ir = await _svc.GetMyNotificationsAsync();
            if (ir.Success) Inbox = new(ir.Data ?? new());

            var sr = await _svc.GetSentAsync();
            if (sr.Success) Sent_ = new(sr.Data ?? new());

            var ur = await _users.GetAllUsersAsync();
            if (ur.Success) AllUsers = new(ur.Data ?? new());
        });
    }

    partial void OnRecipientTypeChanged(string value)
        => IsIndividual = value == "Specific User";

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(NotifTitle)) { SetError("Title is required."); return; }
        if (string.IsNullOrWhiteSpace(NotifMessage)) { SetError("Message is required."); return; }

        await RunBusyAsync(async () =>
        {
            bool ok = RecipientType switch
            {
                "All Users" => (await _svc.SendToAllAsync(NotifTitle, NotifMessage)).Success,
                "All Students" => (await _svc.SendToRoleAsync(Roles.Student, NotifTitle, NotifMessage)).Success,
                "All Supervisors" => (await _svc.SendToRoleAsync(Roles.Supervisor, NotifTitle, NotifMessage)).Success,
                "All Patients" => (await _svc.SendToRoleAsync(Roles.Patient, NotifTitle, NotifMessage)).Success,
                "Specific User" => await SendToIndividualAsync(),
                _ => false
            };

            if (!ok) { SetError("Failed to send notification."); return; }
            Sent = true; NotifTitle = string.Empty; NotifMessage = string.Empty;
            await LoadAsync();
        });
    }

    private async Task<bool> SendToIndividualAsync()
    {
        if (string.IsNullOrWhiteSpace(RecipientEmail)) { SetError("Enter recipient email."); return false; }
        var user = AllUsers.FirstOrDefault(u =>
            u.Email.Equals(RecipientEmail.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null) { SetError("User not found."); return false; }
        var r = await _svc.SendToUserAsync(user.UserId, NotifTitle, NotifMessage);
        return r.Success;
    }

    [RelayCommand]
    private void ClearForm()
    {
        NotifTitle = string.Empty; NotifMessage = string.Empty;
        RecipientEmail = string.Empty; Sent = false;
    }
}

// ══════════════════════════════════════════════════════════════
//  16. ReportsViewModel
// ══════════════════════════════════════════════════════════════
public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reports;

    public record ReportCard(string Icon, string Title, string Description, string Route, string Color);

    public List<ReportCard> Cards { get; } = new()
    {
        new("📋","Booking Report",         "All bookings by date, clinic, and status",         AppRoutes.AdminBookings,    "#1E3A8A"),
        new("📝","Encounter Report",        "All encounters filtered by student and status",    AppRoutes.AdminEncounters,  "#7C3AED"),
        new("📈","PoE Progress Report",    "Student PoE progress across all 5 categories",    AppRoutes.AdminPoe,         "#22C55E"),
        new("👥","Student Activity",        "Bookings accepted, submissions, approvals",        AppRoutes.AdminStudents,    "#F59E0B"),
        new("🩺","Supervisor Workload",    "Encounters signed off per supervisor",             AppRoutes.AdminSupervisors, "#EC4899"),
        new("🔍","Audit Log",               "All system actions with timestamps",               AppRoutes.Audit,            "#6B7280"),
    };

    public ReportsViewModel(IReportService reports)
    { _reports = reports; Title = "Reports"; }

    [RelayCommand]
    private async Task NavigateToReportAsync(ReportCard card)
        => await Shell.Current.GoToAsync($"//{card.Route}");

    [RelayCommand]
    private async Task DownloadPdfAsync(ReportCard card)
    {
        await RunBusyAsync(async () =>
        {
            var r = await _reports.GetReportPdfUrlAsync(card.Title.ToLowerInvariant().Replace(" ", "-"));
            if (!r.Success) { SetError(r.Error!); return; }
            await Launcher.OpenAsync(r.Data!);
        });
    }
}

// ══════════════════════════════════════════════════════════════
//  17. SettingsViewModel
// ══════════════════════════════════════════════════════════════
public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private string _systemName = "UL Optometry Clinical System";
    [ObservableProperty] private string _institution = "University of Limpopo";
    [ObservableProperty] private string _contactEmail = "optometry@ul.ac.za";
    [ObservableProperty] private int _sessionTimeout = 30;
    [ObservableProperty] private int _minPasswordLen = 8;

    // PoE config
    [ObservableProperty] private int _binVisionHrs = 150;
    [ObservableProperty] private int _paediatricHrs = 100;
    [ObservableProperty] private int _contactLensHrs = 100;
    [ObservableProperty] private int _lowVisionHrs = 75;
    [ObservableProperty] private int _ocularDiseaseHrs = 75;

    // SMTP
    [ObservableProperty] private string _smtpHost = "smtp.ul.ac.za";
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _fromEmail = "noreply@ul.ac.za";

    [ObservableProperty] private bool _saved;

    public int TotalPoeHours =>
        BinVisionHrs + PaediatricHrs + ContactLensHrs + LowVisionHrs + OcularDiseaseHrs;

    public SettingsViewModel() { Title = "Settings"; }

    [RelayCommand]
    private async Task SaveGeneralAsync()
    {
        await Task.CompletedTask; // Wire to admin config service when implemented
        Saved = true;
    }

    [RelayCommand]
    private async Task SavePoeAsync()
    {
        OnPropertyChanged(nameof(TotalPoeHours));
        await Task.CompletedTask;
        Saved = true;
    }

    [RelayCommand]
    private async Task SaveSecurityAsync()
    {
        await Task.CompletedTask;
        Saved = true;
    }

    [RelayCommand]
    private async Task SaveEmailAsync()
    {
        await Task.CompletedTask;
        Saved = true;
    }

    [RelayCommand]
    private async Task GoToPageAsync(string route)
        => await Shell.Current.GoToAsync($"//{route}");
}

// ══════════════════════════════════════════════════════════════
//  18. AuditViewModel
// ══════════════════════════════════════════════════════════════
public partial class AuditViewModel : BaseViewModel
{
    private readonly IAuditService _audit;

    private List<AuditLog> _allLogs = new();   // backing list — never filtered in-place

    [ObservableProperty] private ObservableCollection<AuditLog> _logs = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    public AuditViewModel(IAuditService audit) { _audit = audit; Title = "Audit Log"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _audit.GetLogsAsync(limit: 200);
            if (!r.Success) { SetError(r.Error!); return; }
            _allLogs = r.Data ?? new();
            Logs = new(_allLogs);
            IsEmpty = !Logs.Any();
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        // Always filter from _allLogs so backspacing never shrinks the source
        var source = string.IsNullOrWhiteSpace(value)
            ? _allLogs
            : _allLogs.Where(l =>
                l.Action.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                l.UserName.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                l.Entity.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
        Logs = new(source);
        IsEmpty = !Logs.Any();
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _audit.ExportCsvAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            // Share CSV text — in production use FileSaver or Share.RequestAsync
            await Shell.Current.DisplayAlert("Export", "Audit log exported successfully.", "OK");
        });
    }

    [RelayCommand]
    private async Task RowTappedAsync(AuditLog log)
    {
        var route = log.Action.Contains("Booking")
            ? AppRoutes.AdminBookings
            : log.Action.Contains("Encounter") || log.Action.Contains("Approv")
                ? AppRoutes.AdminEncounters
                : log.Action.Contains("User")
                    ? AppRoutes.Users
                    : AppRoutes.Audit;
        await Shell.Current.GoToAsync($"//{route}");
    }
}

// ══════════════════════════════════════════════════════════════
//  BlockedDatesViewModel
// ══════════════════════════════════════════════════════════════
public partial class BlockedDatesViewModel : BaseViewModel
{
    private readonly ISchedulingService _scheduling;
    private readonly IAuthService _auth;

    [ObservableProperty]
    private ObservableCollection<BlockedDate> _blockedDates = new();

    [ObservableProperty] private bool _isEmpty;

    // ── Add form ──────────────────────────────────────────────
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private bool _showAddForm = false;
    [ObservableProperty] private bool _added;

    public BlockedDatesViewModel(ISchedulingService scheduling, IAuthService auth)
    {
        _scheduling = scheduling;
        _auth = auth;
        Title = "Blocked Dates";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.GetBlockedDatesAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            BlockedDates = new(r.Data ?? new());
            IsEmpty = !BlockedDates.Any();
        });
    }

    [RelayCommand]
    private void ShowForm()
    {
        ShowAddForm = true;
        Added = false;
        Reason = string.Empty;
        SelectedDate = DateTime.Today.AddDays(1);
    }

    [RelayCommand]
    private void HideForm() => ShowAddForm = false;

    [RelayCommand]
    private async Task BlockDateAsync()
    {
        if (SelectedDate.Date <= DateTime.Today)
        { SetError("Please select a future date."); return; }

        if (string.IsNullOrWhiteSpace(Reason))
        { SetError("Please enter a reason (e.g. Public Holiday)."); return; }

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.BlockDateAsync(SelectedDate, Reason);
            if (!r.Success) { SetError(r.Error!); return; }
            Added = true;
            ShowAddForm = false;
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task UnblockAsync(BlockedDate blocked)
    {
        bool ok = await Shell.Current.DisplayAlert(
            "Unblock Date",
            $"Unblock {blocked.DateDisplay}? Patients will be able to book on this day again.",
            "Unblock", "Cancel");
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.UnblockDateAsync(blocked.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            await LoadAsync();
        });
    }
}

// ══════════════════════════════════════════════════════════════
//  AdminCalendarDay  — one cell in the admin blocking calendar
// ══════════════════════════════════════════════════════════════
public partial class AdminCalendarDay : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isBlocked;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isSelected;

    public DateTime Date { get; init; }
    public bool IsEmpty { get; init; }
    public bool IsToday => Date.Date == DateTime.Today;
    public bool IsPast => Date.Date < DateTime.Today;
    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    public string DayNumber => IsEmpty ? string.Empty : Date.Day.ToString();
    public int BlockedDateId { get; set; }  // DB id — 0 if not blocked
}

// ══════════════════════════════════════════════════════════════
//  AdminCalendarViewModel  — full calendar for blocking dates
// ══════════════════════════════════════════════════════════════
public partial class AdminCalendarViewModel : BaseViewModel
{
    private readonly ISchedulingService _scheduling;
    private readonly IAuthService _auth;

    [ObservableProperty]
    private ObservableCollection<AdminCalendarDay> _calendarDays = new();

    [ObservableProperty] private DateTime _currentMonth = DateTime.Today;
    [ObservableProperty] private string _monthLabel = string.Empty;

    // Selected day (for the block/unblock panel)
    [ObservableProperty] private AdminCalendarDay? _selectedDay;
    [ObservableProperty] private bool _showPanel;

    // Block form
    [ObservableProperty] private string _reason = string.Empty;
    [ObservableProperty] private bool _actionDone;
    [ObservableProperty] private string _actionMessage = string.Empty;

    // Stats
    [ObservableProperty] private int _blockedThisMonth;
    [ObservableProperty] private int _totalBlocked;

    // All blocked dates loaded from DB
    private List<BlockedDate> _allBlocked = new();

    public AdminCalendarViewModel(ISchedulingService scheduling, IAuthService auth)
    {
        _scheduling = scheduling;
        _auth = auth;
        Title = "Calendar";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.GetBlockedDatesAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            _allBlocked = r.Data ?? new();
            TotalBlocked = _allBlocked.Count;
            BuildCalendar();
        });
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        BuildCalendar();
    }

    [RelayCommand]
    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        BuildCalendar();
    }

    [RelayCommand]
    private void SelectDay(AdminCalendarDay day)
    {
        if (day.IsEmpty) return;

        // Deselect previous
        if (SelectedDay is not null) SelectedDay.IsSelected = false;

        day.IsSelected = true;
        SelectedDay = day;
        ActionDone = false;
        Reason = string.Empty;

        // Show the action panel
        ShowPanel = true;
    }

    [RelayCommand]
    private void ClosePanel()
    {
        if (SelectedDay is not null) SelectedDay.IsSelected = false;
        SelectedDay = null;
        ShowPanel = false;
        ActionDone = false;
    }

    // ── Block a day ───────────────────────────────────────────
    [RelayCommand]
    private async Task BlockDayAsync()
    {
        if (SelectedDay is null) return;

        if (SelectedDay.IsPast)
        { SetError("Cannot block a past date."); return; }

        if (string.IsNullOrWhiteSpace(Reason))
        { SetError("Please enter a reason (e.g. Public Holiday)."); return; }

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.BlockDateAsync(SelectedDay.Date, Reason);
            if (!r.Success) { SetError(r.Error!); return; }

            // Update local state
            SelectedDay.IsBlocked = true;
            SelectedDay.BlockedDateId = r.Data!.Id;
            _allBlocked.Add(r.Data);
            TotalBlocked = _allBlocked.Count;

            ActionDone = true;
            ActionMessage = $"✓ {SelectedDay.Date:dd MMM yyyy} blocked — {Reason}";
            Reason = string.Empty;
            ShowPanel = false;
            BuildCalendar(); // refresh stats
        });
    }

    // ── Unblock a day ─────────────────────────────────────────
    [RelayCommand]
    private async Task UnblockDayAsync()
    {
        if (SelectedDay is null || !SelectedDay.IsBlocked) return;

        bool ok = await Shell.Current.DisplayAlert(
            "Unblock Date",
            $"Allow bookings on {SelectedDay.Date:dd MMM yyyy} again?",
            "Unblock", "Cancel");
        if (!ok) return;

        await RunBusyAsync(async () =>
        {
            var r = await _scheduling.UnblockDateAsync(SelectedDay.BlockedDateId);
            if (!r.Success) { SetError(r.Error!); return; }

            _allBlocked.RemoveAll(b => b.Id == SelectedDay.BlockedDateId);
            TotalBlocked = _allBlocked.Count;

            SelectedDay.IsBlocked = false;
            SelectedDay.BlockedDateId = 0;

            ActionDone = true;
            ActionMessage = $"✓ {SelectedDay.Date:dd MMM yyyy} unblocked.";
            ShowPanel = false;
            BuildCalendar();
        });
    }

    // ── Build calendar cells ──────────────────────────────────
    private void BuildCalendar()
    {
        MonthLabel = CurrentMonth.ToString("MMMM yyyy");
        var days = new ObservableCollection<AdminCalendarDay>();
        var first = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        // Monday-first padding
        int startDow = ((int)first.DayOfWeek + 6) % 7;
        for (int i = 0; i < startDow; i++)
            days.Add(new AdminCalendarDay { IsEmpty = true });

        int blockedCount = 0;
        for (var d = first; d <= last; d = d.AddDays(1))
        {
            var blocked = _allBlocked.FirstOrDefault(
                b => b.Date.Date == d.Date);
            bool isBlocked = blocked is not null;
            if (isBlocked && d.Month == CurrentMonth.Month) blockedCount++;

            days.Add(new AdminCalendarDay
            {
                Date = d,
                IsBlocked = isBlocked,
                BlockedDateId = blocked?.Id ?? 0,
            });
        }

        BlockedThisMonth = blockedCount;
        CalendarDays = days;
    }
}

// ══════════════════════════════════════════════════════════════
//  AdminBookPatientViewModel
//  Admin books on behalf of a walk-in or follow-up patient.
//  Mirrors the patient booking flow exactly:
//    Step 1 — Select / create patient
//    Step 2 — Select clinic
//    Step 3 — Select date  (only clinic weekdays, no blocked dates)
//    Step 4 — Select time slot
//    Step 5 — Confirm
// ══════════════════════════════════════════════════════════════
public partial class AdminBookPatientViewModel : BaseViewModel
{
    private readonly IAdminBookingService _adminBookings;
    private readonly IBookingService _bookings;
    private readonly IUserService _users;
    private readonly IAuthService _auth;
    private readonly ISchedulingService _scheduling;

    // ── Wizard step ───────────────────────────────────────────
    [ObservableProperty] private int _currentStep = 1;
    [ObservableProperty] private bool _isFirstStep = true;
    [ObservableProperty] private bool _isLastStep = false;
    [ObservableProperty] private bool _bookingDone = false;
    [ObservableProperty] private Guid _newBookingId;

    // ── Step 1 — Patient ──────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<UserProfile> _patients = new();
    [ObservableProperty] private UserProfile? _selectedPatient;
    [ObservableProperty] private string _patientSearch = string.Empty;

    // Walk-in: create a new patient on the fly
    [ObservableProperty] private bool _showCreatePatient = false;
    [ObservableProperty] private string _newPatientName = string.Empty;
    [ObservableProperty] private string _newPatientPhone = string.Empty;
    [ObservableProperty] private string _newPatientEmail = string.Empty;

    // ── Booking type ──────────────────────────────────────────
    [ObservableProperty] private string _bookingType = "WalkIn"; // WalkIn | FollowUp
    public List<string> BookingTypes { get; } = new() { "Walk-In", "Follow-Up" };

    // ── Step 2 — Clinic ───────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Clinic> _clinics = new();
    [ObservableProperty] private Clinic? _selectedClinic;

    // ── Step 3 — Date (calendar) ──────────────────────────────
    [ObservableProperty]
    private ObservableCollection<CalendarDay> _calendarDays = new();
    [ObservableProperty] private DateTime _currentMonth = DateTime.Today;
    [ObservableProperty] private string _monthLabel = string.Empty;
    [ObservableProperty] private CalendarDay? _selectedDay;
    private List<DateTime> _blockedDates = new();

    // ── Step 4 — Slot ─────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Session> _slots = new();
    [ObservableProperty] private Session? _selectedSlot;

    // ── Step 5 — Confirm ──────────────────────────────────────
    // (all bound properties are already set by steps 1–4)

    // ── Display helpers ───────────────────────────────────────
    public string DateDisplay =>
        SelectedDay is not null
            ? SelectedDay.Date.ToString("dd MMMM yyyy")
            : string.Empty;

    public string BookingTypeBadge =>
        BookingType == "WalkIn" ? "Walk-In" : "Follow-Up";

    public string SelectedPatientName => SelectedPatient?.FullName ?? string.Empty;

    public string SelectedClinicName => SelectedClinic?.Name ?? string.Empty;

    public string SelectedSlotDisplay => SelectedSlot?.Display ?? string.Empty;

    public AdminBookPatientViewModel(
        IAdminBookingService adminBookings,
        IBookingService bookings,
        IUserService users,
        IAuthService auth,
        ISchedulingService scheduling)
    {
        _adminBookings = adminBookings;
        _bookings = bookings;
        _users = users;
        _auth = auth;
        _scheduling = scheduling;
        Title = "Book for Patient";
    }

    // ── Load ──────────────────────────────────────────────────
    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            // Load existing patients
            var pr = await _users.GetAllUsersAsync("patient");
            if (pr.Success)
                Patients = new(pr.Data ?? new());

            // Load clinics
            var cr = await _bookings.GetClinicsAsync();
            if (cr.Success)
                Clinics = new(cr.Data ?? new());

            // Load blocked dates for calendar
            var br = await _scheduling.GetBlockedDatesAsync();
            if (br.Success) _blockedDates = br.Data?.Select(b => b.Date).ToList() ?? new();
        });
    }

    // ── Step navigation ───────────────────────────────────────
    [RelayCommand]
    private void NextStep()
    {
        if (!ValidateCurrentStep()) return;
        if (CurrentStep >= 5) return;
        CurrentStep++;
        UpdateStepFlags();

        // Trigger side-effects per step
        if (CurrentStep == 3 && SelectedClinic is not null)
            BuildCalendar();
        if (CurrentStep == 4)
            _ = LoadSlotsAsync();
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep <= 1) return;
        CurrentStep--;
        UpdateStepFlags();
    }

    [RelayCommand]
    private void GoToStep(int step) { CurrentStep = step; UpdateStepFlags(); }

    private void UpdateStepFlags()
    {
        IsFirstStep = CurrentStep == 1;
        IsLastStep = CurrentStep == 5;
        Title = $"Book for Patient — Step {CurrentStep} of 5";
        OnPropertyChanged(nameof(DateDisplay));
    }

    private bool ValidateCurrentStep()
    {
        ClearError();
        switch (CurrentStep)
        {
            case 1:
                if (SelectedPatient is null && !ShowCreatePatient)
                { SetError("Please select a patient or create a walk-in."); return false; }
                if (ShowCreatePatient && string.IsNullOrWhiteSpace(NewPatientName))
                { SetError("Walk-in patient name is required."); return false; }
                return true;
            case 2:
                if (SelectedClinic is null)
                { SetError("Please select a clinic."); return false; }
                return true;
            case 3:
                if (SelectedDay is null)
                { SetError("Please select a date."); return false; }
                return true;
            case 4:
                if (SelectedSlot is null)
                { SetError("Please select a time slot."); return false; }
                return true;
            default:
                return true;
        }
    }

    // ── Step 1 — Patient search ───────────────────────────────
    partial void OnSelectedPatientChanged(UserProfile? value)
        => OnPropertyChanged(nameof(SelectedPatientName));

    partial void OnSelectedClinicChanged(Clinic? value)
        => OnPropertyChanged(nameof(SelectedClinicName));

    partial void OnSelectedSlotChanged(Session? value)
        => OnPropertyChanged(nameof(SelectedSlotDisplay));
    partial void OnPatientSearchChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadAsync();
            return;
        }
        var q = value.ToLowerInvariant();
        Patients = new(Patients.Where(p =>
            p.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            p.Email.Contains(q, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private void SelectPatient(UserProfile patient)
    {
        SelectedPatient = patient;
        ShowCreatePatient = false;
        ClearError();
    }

    [RelayCommand]
    private void ToggleCreatePatient()
    {
        ShowCreatePatient = !ShowCreatePatient;
        if (ShowCreatePatient) SelectedPatient = null;
    }

    [RelayCommand]
    private void SetBookingType(string type)
    {
        BookingType = type;
        OnPropertyChanged(nameof(BookingTypeBadge));
    }

    [RelayCommand]
    private async Task CreateWalkInPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPatientName))
        { SetError("Name is required for walk-in patient."); return; }

        await RunBusyAsync(async () =>
        {
            // Create a minimal patient profile
            var r = await _users.CreateUserAsync(new CreateUserRequest
            {
                FullName = NewPatientName.Trim(),
                Email = string.IsNullOrWhiteSpace(NewPatientEmail)
                                    ? $"walkin_{DateTime.Now:yyyyMMddHHmmss}@walkin.ul.ac.za"
                                    : NewPatientEmail.Trim(),
                Phone = NewPatientPhone.Trim(),
                DefaultPassword = "WalkIn@1234",
                Role = UserRole.Patient,
            });

            if (!r.Success) { SetError(r.Error!); return; }

            SelectedPatient = r.Data;
            ShowCreatePatient = false;
            NewPatientName = string.Empty;
            NewPatientPhone = string.Empty;
            NewPatientEmail = string.Empty;
        });
    }

    // ── Step 2 — Clinic selection ─────────────────────────────
    [RelayCommand]
    private void SelectClinic(Clinic clinic)
    {
        foreach (var c in Clinics) c.IsSelected = false;
        clinic.IsSelected = true;
        SelectedClinic = clinic;
        Clinics = new(Clinics);
        ClearError();
    }

    // ── Step 3 — Calendar ─────────────────────────────────────
    [RelayCommand]
    private void PreviousMonth()
    { CurrentMonth = CurrentMonth.AddMonths(-1); BuildCalendar(); }

    [RelayCommand]
    private void NextMonth()
    { CurrentMonth = CurrentMonth.AddMonths(1); BuildCalendar(); }

    [RelayCommand]
    private void SelectDay(CalendarDay day)
    {
        if (!day.IsSelectable) return;
        if (SelectedDay is not null) SelectedDay.IsSelected = false;
        day.IsSelected = true;
        SelectedDay = day;
        OnPropertyChanged(nameof(DateDisplay));
        ClearError();
    }

    private void BuildCalendar()
    {
        if (SelectedClinic is null) return;
        MonthLabel = CurrentMonth.ToString("MMMM yyyy");
        var days = new ObservableCollection<CalendarDay>();
        var first = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        int startDow = ((int)first.DayOfWeek + 6) % 7;
        for (int i = 0; i < startDow; i++)
            days.Add(new CalendarDay { IsEmpty = true });

        for (var d = first; d <= last; d = d.AddDays(1))
        {
            int dow = ((int)d.DayOfWeek + 6) % 7;
            bool isClinic = (dow + 1) == SelectedClinic.Weekday;
            bool isBlocked = _blockedDates.Any(b => b.Date == d.Date);

            days.Add(new CalendarDay
            {
                Date = d,
                IsSelectable = isClinic && d.Date >= DateTime.Today && !isBlocked,
                IsClinicDay = isClinic,
                IsPast = d.Date < DateTime.Today,
                IsBlocked = isBlocked,
            });
        }
        CalendarDays = days;
    }

    // ── Step 4 — Time slots ───────────────────────────────────
    private async Task LoadSlotsAsync()
    {
        if (SelectedDay is null || SelectedClinic is null) return;
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.GetAvailableSlotsAsync(
                SelectedDay.Date, SelectedClinic.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            Slots = new(r.Data ?? new());
            if (!Slots.Any()) SetError("No available slots for this date.");
        });
    }

    [RelayCommand]
    private void SelectSlot(Session slot)
    {
        SelectedSlot = slot;
        ClearError();
    }

    // ── Step 5 — Confirm & book ───────────────────────────────
    [RelayCommand]
    private async Task ConfirmBookingAsync()
    {
        if (SelectedPatient is null || SelectedClinic is null ||
            SelectedDay is null || SelectedSlot is null)
        { SetError("Please complete all steps before confirming."); return; }

        await RunBusyAsync(async () =>
        {
            // Book in the selected patient's name, not the admin's identity
            Guid.TryParse(_auth.CurrentUserId, out var adminId);
            var r = await _adminBookings.BookForPatientAsync(
                new AdminBookForPatientRequest
                {
                    PatientId     = SelectedPatient!.UserId,
                    ClinicId      = SelectedClinic!.Id,
                    SessionId     = SelectedSlot!.Id,
                    Date          = SelectedDay!.Date,
                    BookingType   = BookingType,
                    BookedByAdmin = adminId == Guid.Empty ? null : adminId,
                });

            if (!r.Success) { SetError(r.Error!); return; }

            NewBookingId = r.Data!.Id;
            BookingDone = true;
        });
    }

    [RelayCommand]
    private void ResetForm()
    {
        CurrentStep = 1;
        SelectedPatient = null;
        SelectedClinic = null;
        SelectedDay = null;
        SelectedSlot = null;
        BookingDone = false;
        ShowCreatePatient = false;
        BookingType = "WalkIn";
        UpdateStepFlags();
        BuildCalendar();
    }
}
