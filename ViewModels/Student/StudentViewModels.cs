// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Student/StudentViewModels.cs
//  All 13 Student portal ViewModels.
// ════════════════════════════════════════════════════════════════════════

using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Constants;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models;

using UL_Optometry.Models.Notification;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;

namespace UL_Optometry.ViewModels.Student;

// ══════════════════════════════════════════════════════════════
//  1. StudentDashboardViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentDashboardViewModel : BaseViewModel
{
    private readonly IQueueService        _queue;
    private readonly IEncounterService    _encounters;
    private readonly IPoeService          _poe;
    private readonly IAuthService         _auth;
    private readonly INotificationService _notifications;

    [ObservableProperty] private string  _greeting          = string.Empty;
    [ObservableProperty] private int     _queueCount;
    [ObservableProperty] private int     _acceptedCount;
    [ObservableProperty] private int     _pendingReviewCount;
    [ObservableProperty] private double  _poePercent;
    [ObservableProperty] private int     _unreadCount;

    // Full-width queue on dashboard — no Quick Access card
    [ObservableProperty] private ObservableCollection<Booking> _queueItems = new();
    [ObservableProperty] private bool _queueIsEmpty;

    public StudentDashboardViewModel(
        IQueueService queue, IEncounterService encounters,
        IPoeService poe, IAuthService auth, INotificationService notifications)
    {
        _queue         = queue;
        _encounters    = encounters;
        _poe           = poe;
        _auth          = auth;
        _notifications = notifications;
        Title          = "Dashboard";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var name = _auth.CurrentUser?.FullName.Split(' ')[0] ?? "Student";
            Greeting = $"Hello, {name} 👋";

            var qr = await _queue.GetQueueAsync();
            if (qr.Success)
            {
                QueueItems  = new(qr.Data ?? new());
                QueueCount  = QueueItems.Count;
                QueueIsEmpty = !QueueItems.Any();
            }

            var ar = await _queue.GetAcceptedAsync();
            if (ar.Success) AcceptedCount = ar.Data?.Count ?? 0;

            var er = await _encounters.GetMyEncountersAsync();
            if (er.Success)
                PendingReviewCount = er.Data?
                    .Count(e => e.EncounterStatus == EncounterStatus.Submitted ||
                                e.EncounterStatus == EncounterStatus.UnderReview) ?? 0;

            var pr = await _poe.GetPoeSummaryAsync();
            if (pr.Success) PoePercent = pr.Data?.OverallPercent ?? 0;

            var nr = await _notifications.GetMyNotificationsAsync();
            if (nr.Success) UnreadCount = nr.Data?.Count(n => !n.IsRead) ?? 0;
        });
    }

    [RelayCommand]
    private async Task AcceptBookingAsync(Booking booking)
    {
        await RunBusyAsync(async () =>
        {
            var result = await _queue.AcceptBookingAsync(booking.Id);
            if (!result.Success) { SetError(result.Error!); return; }
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task GoToBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentBookings}");

    [RelayCommand]
    private async Task GoToEncountersAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentEncounters}");

    [RelayCommand]
    private async Task GoToPoeAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentPoe}");
}

// ══════════════════════════════════════════════════════════════
//  2. StudentBookingsViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentBookingsViewModel : BaseViewModel
{
    private readonly IQueueService _queue;

    [ObservableProperty] private ObservableCollection<Booking> _queueItems    = new();
    [ObservableProperty] private ObservableCollection<Booking> _acceptedItems = new();
    [ObservableProperty] private ObservableCollection<Booking> _inProgress    = new();
    [ObservableProperty] private ObservableCollection<Booking> _completed     = new();
    [ObservableProperty] private bool _queueIsEmpty, _acceptedIsEmpty, _inProgressIsEmpty, _completedIsEmpty;

    public StudentBookingsViewModel(IQueueService queue)
    { _queue = queue; Title = "My Bookings"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var qr = await _queue.GetQueueAsync();
            if (qr.Success) { QueueItems   = new(qr.Data ?? new()); QueueIsEmpty    = !QueueItems.Any(); }

            var ar = await _queue.GetAcceptedAsync();
            if (ar.Success) { AcceptedItems = new(ar.Data ?? new()); AcceptedIsEmpty  = !AcceptedItems.Any(); }

            var ir = await _queue.GetInProgressAsync();
            if (ir.Success) { InProgress    = new(ir.Data ?? new()); InProgressIsEmpty = !InProgress.Any(); }

            var cr = await _queue.GetCompletedAsync();
            if (cr.Success) { Completed     = new(cr.Data ?? new()); CompletedIsEmpty  = !Completed.Any(); }
        });
    }

    [RelayCommand]
    private async Task AcceptBookingAsync(Booking booking)
    {
        await RunBusyAsync(async () =>
        {
            var r = await _queue.AcceptBookingAsync(booking.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            await LoadAsync();
        });
    }

    [RelayCommand]
    private async Task ViewBookingAsync(Booking booking)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.StudentBookingDetail}?bookingId={booking.Id}");
}

// ══════════════════════════════════════════════════════════════
//  3. StudentBookingDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class StudentBookingDetailViewModel : BaseViewModel
{
    private readonly IQueueService _queue;

    [ObservableProperty] private string   _bookingId = string.Empty;
    [ObservableProperty] private Booking? _booking;
    [ObservableProperty] private bool     _canAccept;
    [ObservableProperty] private bool     _canCancel;
    [ObservableProperty] private bool     _canStartEncounter;
    [ObservableProperty] private bool     _accepted;

    public StudentBookingDetailViewModel(IQueueService queue)
    { _queue = queue; Title = "Booking Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(BookingId, out var id)) return;
            var r = await _queue.GetBookingByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Booking            = r.Data;
            CanAccept          = Booking?.BookingStatus == BookingStatus.Pending;
            CanCancel          = Booking?.BookingStatus == BookingStatus.Accepted;
            CanStartEncounter  = Booking?.BookingStatus == BookingStatus.Accepted;
        });
    }

    [RelayCommand]
    private async Task AcceptAsync()
    {
        if (Booking is null) return;
        await RunBusyAsync(async () =>
        {
            var r = await _queue.AcceptBookingAsync(Booking.Id);
            if (!r.Success) { SetError(r.Error!); return; }
            Accepted           = true;
            CanAccept          = false;
            CanCancel          = true;
            CanStartEncounter  = true;
            Booking.Status     = BookingStatuses.Accepted;
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (Booking is null) return;
        await Shell.Current.GoToAsync(
            $"{AppRoutes.StudentCancelBooking}?bookingId={Booking.Id}");
    }

    [RelayCommand]
    private async Task StartEncounterAsync()
    {
        if (Booking is null) return;
        await Shell.Current.GoToAsync(
            $"{AppRoutes.EncounterTypeSelect}?bookingId={Booking.Id}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  4. StudentCancelBookingViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class StudentCancelBookingViewModel : BaseViewModel
{
    private readonly IQueueService _queue;

    [ObservableProperty] private string _bookingId      = string.Empty;
    [ObservableProperty] private string _selectedReason = string.Empty;
    [ObservableProperty] private string _notes          = string.Empty;
    [ObservableProperty] private bool   _cancelled;

    public List<string> Reasons { get; } = new()
    { "Schedule conflict", "Health / illness", "Transport issues",
      "Accepted by mistake", "Other" };

    public StudentCancelBookingViewModel(IQueueService queue)
    { _queue = queue; Title = "Cancel Booking"; }

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedReason))
        { SetError("Please select a reason."); return; }
        if (!Guid.TryParse(BookingId, out var id)) return;

        await RunBusyAsync(async () =>
        {
            var r = await _queue.CancelAcceptedBookingAsync(id, SelectedReason);
            if (!r.Success) { SetError(r.Error!); return; }
            Cancelled = true;
        });
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
    [RelayCommand] private async Task GoToBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentBookings}");
}

// ══════════════════════════════════════════════════════════════
//  5. StudentEncountersViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentEncountersViewModel : BaseViewModel
{
    private readonly IEncounterService _encounters;

    [ObservableProperty] private ObservableCollection<Encounter> _all       = new();
    [ObservableProperty] private ObservableCollection<Encounter> _drafts    = new();
    [ObservableProperty] private ObservableCollection<Encounter> _submitted = new();
    [ObservableProperty] private ObservableCollection<Encounter> _approved  = new();
    [ObservableProperty] private ObservableCollection<Encounter> _revisions = new();
    [ObservableProperty] private bool _isEmpty;

    public StudentEncountersViewModel(IEncounterService encounters)
    { _encounters = encounters; Title = "My Encounters"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _encounters.GetMyEncountersAsync();
            if (!r.Success) { SetError(r.Error!); return; }

            var data = r.Data ?? new();
            All       = new(data);
            Drafts    = new(data.Where(e => e.EncounterStatus == EncounterStatus.Draft));
            Submitted = new(data.Where(e => e.EncounterStatus is
                EncounterStatus.Submitted or EncounterStatus.UnderReview));
            Approved  = new(data.Where(e => e.EncounterStatus == EncounterStatus.Approved));
            Revisions = new(data.Where(e => e.EncounterStatus == EncounterStatus.Revision));
            IsEmpty   = !All.Any();
        });
    }

    [RelayCommand]
    private async Task ViewEncounterAsync(Encounter encounter)
        => await Shell.Current.GoToAsync(
            $"{AppRoutes.StudentEncounterDetail}?encounterId={encounter.Id}");

    [RelayCommand]
    private async Task StartNewEncounterAsync()
        => await Shell.Current.GoToAsync(AppRoutes.EncounterTypeSelect);
}

// ══════════════════════════════════════════════════════════════
//  6. EncounterTypeSelectViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class EncounterTypeSelectViewModel : BaseViewModel
{
    [ObservableProperty] private string _bookingId    = string.Empty;
    [ObservableProperty] private string _selectedType = string.Empty;   // "Onsite" | "Offsite"

    public EncounterTypeSelectViewModel() { Title = "New Encounter"; }

    [RelayCommand]
    private async Task SelectOnsiteAsync()
    {
        SelectedType = EncounterTypes.Onsite;
        await Shell.Current.GoToAsync(
            $"{AppRoutes.EncounterForm}?bookingId={BookingId}&encounterType={EncounterTypes.Onsite}");
    }

    [RelayCommand]
    private async Task SelectOffsiteAsync()
    {
        SelectedType = EncounterTypes.Offsite;
        await Shell.Current.GoToAsync(
            $"{AppRoutes.EncounterForm}?bookingId={BookingId}&encounterType={EncounterTypes.Offsite}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  7. EncounterFormViewModel  (6-step wizard)
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(BookingId),      "bookingId")]
[QueryProperty(nameof(EncounterType),  "encounterType")]
[QueryProperty(nameof(EncounterId),    "encounterId")]   // set when resuming a draft
public partial class EncounterFormViewModel : BaseViewModel
{
    private readonly IEncounterService _encounters;
    private readonly IPoeService       _poe;
    private readonly IUserService      _users;
    private readonly ISchedulingService _scheduling;

    // ── Wizard state ──────────────────────────────────────────────────
    [ObservableProperty] private int    _currentStep   = 1;
    [ObservableProperty] private bool   _isFirstStep   = true;
    [ObservableProperty] private bool   _isLastStep    = false;
    [ObservableProperty] private bool   _draftSaved;
    [ObservableProperty] private bool   _submitted;

    // ── Query params ──────────────────────────────────────────────────
    [ObservableProperty] private string _bookingId     = string.Empty;
    [ObservableProperty] private string _encounterType = EncounterTypes.Onsite;
    [ObservableProperty] private string _encounterId   = string.Empty;

    public bool IsOffsite => EncounterType == EncounterTypes.Offsite;

    // ── Read-only booking-prepopulated display values (Step 1 grey panel) ─
    private string _bookingClinicName  = string.Empty;
    private string _bookingSlotDisplay = string.Empty;
    private string _bookingCubicle     = string.Empty;
    private int? _bookingSessionId;
    private Guid? _pendingSupervisorId;
    private int? _pendingCubicleId;
    private bool _isSyncingOnsiteSelection;
    public string BookingClinicName  => _bookingClinicName;
    public string BookingSlotDisplay => _bookingSlotDisplay;
    public string BookingCubicle     => _bookingCubicle;
    public bool   HasBookingContext  => !string.IsNullOrEmpty(BookingId);

    [ObservableProperty] private ObservableCollection<SupervisorProfile> _onsiteSupervisors = new();
    [ObservableProperty] private ObservableCollection<CubicleAssignment> _onsiteCubicles = new();
    [ObservableProperty] private SupervisorProfile? _selectedOnsiteSupervisor;
    [ObservableProperty] private CubicleAssignment? _selectedCubicleAssignment;
    public string SelectedOnsiteSupervisorName => SelectedOnsiteSupervisor?.FullName ?? "No supervisor selected";
    public bool HasSelectedOnsiteSupervisor => SelectedOnsiteSupervisor is not null;

    // ── PoE categories (Step 4) ───────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<PoeCategorySelection> _poeCategories = new();

    // ════════════════════════════════════════════════════════
    //  SECTION 1 — Patient & Exam Info
    // ════════════════════════════════════════════════════════
    [ObservableProperty] private string _patientFileNumber       = string.Empty;
    [ObservableProperty] private string _patientName             = string.Empty;
    [ObservableProperty] private string _dob                     = string.Empty;
    [ObservableProperty] private string _gender                  = string.Empty;
    [ObservableProperty] private string _examType                = string.Empty;
    [ObservableProperty] private string _encounterTime           = string.Empty;
    [ObservableProperty] private string _location                = string.Empty;
    [ObservableProperty] private string _reasonForVisit          = string.Empty;
    [ObservableProperty] private string _history                 = string.Empty;
    [ObservableProperty] private string _ocularHistory           = string.Empty;
    [ObservableProperty] private string _offSiteSupervisorName   = string.Empty;
    [ObservableProperty] private string _offSiteOpNumber         = string.Empty;

    // ════════════════════════════════════════════════════════
    //  SECTION 2 — Clinical Findings
    // ════════════════════════════════════════════════════════
    [ObservableProperty] private string _distanceVaOD    = string.Empty;
    [ObservableProperty] private string _distanceVaOS    = string.Empty;
    [ObservableProperty] private string _nearVaOD        = string.Empty;
    [ObservableProperty] private string _nearVaOS        = string.Empty;
    [ObservableProperty] private string _refractionOD    = string.Empty;
    [ObservableProperty] private string _refractionOS    = string.Empty;
    [ObservableProperty] private string _npcBreak        = string.Empty;
    [ObservableProperty] private string _npcRecovery     = string.Empty;
    [ObservableProperty] private string _coverTestDistance = string.Empty;
    [ObservableProperty] private string _coverTestNear   = string.Empty;
    [ObservableProperty] private string _anteriorSegment = string.Empty;
    [ObservableProperty] private string _posteriorSegment= string.Empty;
    [ObservableProperty] private string _additionalFindings = string.Empty;

    // ════════════════════════════════════════════════════════
    //  SECTION 3 — Impression & Plan
    // ════════════════════════════════════════════════════════
    [ObservableProperty] private string _diagnosis             = string.Empty;
    [ObservableProperty] private string _differentialDiagnosis = string.Empty;
    [ObservableProperty] private string _managementPlan        = string.Empty;
    [ObservableProperty] private string _referral              = string.Empty;
    [ObservableProperty] private string _followUp              = string.Empty;
    [ObservableProperty] private string _treatmentNotes        = string.Empty;

    // ════════════════════════════════════════════════════════
    //  SECTION 4 — Reflection & PoE
    // ════════════════════════════════════════════════════════
    [ObservableProperty] private string _studentReflection = string.Empty;
    [ObservableProperty] private bool   _confirmAccurate;

    // Picker helpers
    public List<string> GenderOptions { get; } = new() { "Male", "Female", "Other" };
    public List<string> ExamTypeOptions { get; } = new()
    { "Comprehensive Eye Exam", "Binocular Vision Assessment", "Contact Lens Assessment",
      "Low Vision Assessment", "Paediatric Eye Exam", "Follow-Up" };

    public EncounterFormViewModel(
        IEncounterService encounters,
        IPoeService poe,
        IUserService users,
        ISchedulingService scheduling)
    {
        _encounters = encounters;
        _poe        = poe;
        _users      = users;
        _scheduling = scheduling;
        Title       = "New Encounter";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            // Load PoE categories for step 4 checkboxes
            var cats = await _poe.GetCategoriesAsync();
            if (cats.Success)
            {
                PoeCategories = new ObservableCollection<PoeCategorySelection>(
                    cats.Data!.Select(c => new PoeCategorySelection
                    {
                        CategoryId = c.Id,
                        Name       = c.Name,
                        HexColor   = c.HexColor,
                    }));
            }

            // If resuming a draft, load its values
            if (!string.IsNullOrEmpty(EncounterId) &&
                Guid.TryParse(EncounterId, out var id))
            {
                var dr = await _encounters.GetEncounterByIdAsync(id);
                if (dr.Success && dr.Data is not null)
                    PopulateFromEncounter(dr.Data);
            }
            // Starting fresh from a booking — prepopulate read-only fields
            else if (!string.IsNullOrEmpty(BookingId) &&
                     Guid.TryParse(BookingId, out var bid))
            {
                var pr = await _encounters.GetPrepopulatedFromBookingAsync(bid);
                if (pr.Success && pr.Data is not null)
                    PopulateFromBookingPrepopulated(pr.Data);
            }

            await LoadOnsiteSelectorsAsync();
            UpdateStepFlags();
        });
    }

    // ── Prepopulate read-only fields from booking ─────────────────────
    private void PopulateFromBookingPrepopulated(Encounter e)
    {
        PatientName       = e.PatientName;
        PatientFileNumber = e.PatientFileNumber;
        Dob               = e.Dob;
        Gender            = e.Gender;
        _bookingSessionId = e.SessionId;
        _pendingCubicleId = e.CubicleId;
        _pendingSupervisorId = e.SupervisorId;
        if (e.CubicleId.HasValue && !string.IsNullOrWhiteSpace(e.CubicleNumber))
            Location = e.CubicleNumber;
        // Store joined display values so XAML read-only labels show them
        // The Encounter's joined fields are surfaced via BookingClinicName etc.
        _bookingClinicName   = e.ClinicName;
        _bookingSlotDisplay  = e.SlotDisplay;
        _bookingCubicle      = e.CubicleNumber;
        OnPropertyChanged(nameof(BookingClinicName));
        OnPropertyChanged(nameof(BookingSlotDisplay));
        OnPropertyChanged(nameof(BookingCubicle));
    }

    private async Task LoadOnsiteSelectorsAsync()
    {
        var supervisorsResult = await _users.GetSupervisorsAsync();
        if (supervisorsResult.Success)
            OnsiteSupervisors = new(supervisorsResult.Data ?? new());

        var sessionIdForLookup = _bookingSessionId;
        if (!sessionIdForLookup.HasValue)
        {
            var sessions = await _scheduling.GetSessionsAsync();
            sessionIdForLookup = sessions.Success ? sessions.Data?.FirstOrDefault()?.Id : null;
        }

        if (sessionIdForLookup.HasValue)
        {
            var assignmentsResult = await _scheduling.GetAssignmentsAsync(sessionIdForLookup.Value);
            if (assignmentsResult.Success)
                OnsiteCubicles = new(assignmentsResult.Data ?? new());
        }
        else
        {
            OnsiteCubicles = new();
        }

        if (_pendingCubicleId.HasValue)
            SelectedCubicleAssignment = OnsiteCubicles.FirstOrDefault(c => c.CubicleId == _pendingCubicleId.Value);

        if (_pendingSupervisorId.HasValue)
            SelectedOnsiteSupervisor = OnsiteSupervisors.FirstOrDefault(s => s.UserId == _pendingSupervisorId.Value);

        if (SelectedOnsiteSupervisor is null && SelectedCubicleAssignment?.SupervisorId.HasValue == true)
            SelectedOnsiteSupervisor = OnsiteSupervisors.FirstOrDefault(s => s.UserId == SelectedCubicleAssignment.SupervisorId.Value);

        OnPropertyChanged(nameof(SelectedOnsiteSupervisorName));
        OnPropertyChanged(nameof(HasSelectedOnsiteSupervisor));
    }

    partial void OnSelectedCubicleAssignmentChanged(CubicleAssignment? value)
    {
        if (_isSyncingOnsiteSelection) return;
        _isSyncingOnsiteSelection = true;
        try
        {
            Location = value?.CubicleName ?? string.Empty;
            _pendingCubicleId = value?.CubicleId;

            if (value?.SupervisorId.HasValue == true)
            {
                SelectedOnsiteSupervisor = OnsiteSupervisors
                    .FirstOrDefault(s => s.UserId == value.SupervisorId.Value);
                _pendingSupervisorId = SelectedOnsiteSupervisor?.UserId;
            }
            else
            {
                SelectedOnsiteSupervisor = null;
                _pendingSupervisorId = null;
            }
        }
        finally
        {
            _isSyncingOnsiteSelection = false;
            OnPropertyChanged(nameof(SelectedOnsiteSupervisorName));
            OnPropertyChanged(nameof(HasSelectedOnsiteSupervisor));
        }
    }

    partial void OnSelectedOnsiteSupervisorChanged(SupervisorProfile? value)
    {
        if (_isSyncingOnsiteSelection) return;
        _isSyncingOnsiteSelection = true;
        try
        {
            _pendingSupervisorId = value?.UserId;

            if (value is null) return;

            var linkedCubicle = OnsiteCubicles.FirstOrDefault(c => c.SupervisorId == value.UserId);
            if (linkedCubicle is not null)
            {
                SelectedCubicleAssignment = linkedCubicle;
                Location = linkedCubicle.CubicleName;
                _pendingCubicleId = linkedCubicle.CubicleId;
            }
            else
            {
                SelectedCubicleAssignment = null;
                Location = string.Empty;
                _pendingCubicleId = null;
            }
        }
        finally
        {
            _isSyncingOnsiteSelection = false;
            OnPropertyChanged(nameof(SelectedOnsiteSupervisorName));
            OnPropertyChanged(nameof(HasSelectedOnsiteSupervisor));
        }
    }

    // ── Navigation ────────────────────────────────────────────────────
    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep >= 6) return;

        // Step 1 offsite guard: both supervisor fields required
        if (CurrentStep == 1 && IsOffsite)
        {
            if (string.IsNullOrWhiteSpace(OffSiteSupervisorName))
            { SetError("Supervising optometrist name is required for offsite encounters."); return; }
            if (string.IsNullOrWhiteSpace(OffSiteOpNumber))
            { SetError("Supervising optometrist HPCSA/OP number is required for offsite encounters."); return; }
        }

        ClearError();
        CurrentStep++;
        UpdateStepFlags();
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep <= 1) return;
        CurrentStep--;
        UpdateStepFlags();
    }

    [RelayCommand]
    private void GoToStep(int step)
    {
        if (step is < 1 or > 6) return;
        CurrentStep = step;
        UpdateStepFlags();
    }

    private void UpdateStepFlags()
    {
        IsFirstStep = CurrentStep == 1;
        IsLastStep  = CurrentStep == 6;
        Title       = $"Encounter — Step {CurrentStep} of 6";
    }

    // ── Draft save ────────────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        await RunBusyAsync(async () =>
        {
            var encounter = BuildEncounter();
            encounter.Status = EncounterStatuses.Draft;
            var r = await _encounters.SaveDraftAsync(encounter);
            if (!r.Success) { SetError(r.Error!); return; }
            EncounterId = r.Data!.Id.ToString();
            DraftSaved  = true;
        });
    }

    // ── Submit ────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task SubmitAsync()
    {
        // Step 6 validation
        if (!ConfirmAccurate)
        { SetError("Please confirm that all information is accurate."); return; }

        if (string.IsNullOrWhiteSpace(PatientName))
        { SetError("Patient name is required (Step 1)."); return; }

        if (IsOffsite && string.IsNullOrWhiteSpace(OffSiteOpNumber))
        { SetError("Supervising optometrist OP number is required for offsite encounters."); return; }
        if (!IsOffsite && SelectedOnsiteSupervisor is null)
        { SetError("Please select a supervisor for this onsite encounter."); return; }

        await RunBusyAsync(async () =>
        {
            var encounter = BuildEncounter();
            var r = await _encounters.SubmitEncounterAsync(encounter);
            if (!r.Success) { SetError(r.Error!); return; }

            await Shell.Current.GoToAsync(
                $"{AppRoutes.EncounterSubmitSuccess}?encounterId={r.Data!.Id}&isOffsite={IsOffsite}");
        });
    }

    // ── Resubmit (after revision) ─────────────────────────────────────
    [RelayCommand]
    private async Task ResubmitAsync()
    {
        await RunBusyAsync(async () =>
        {
            var encounter = BuildEncounter();
            var r = await _encounters.ResubmitEncounterAsync(encounter);
            if (!r.Success) { SetError(r.Error!); return; }

            await Shell.Current.GoToAsync(
                $"{AppRoutes.EncounterSubmitSuccess}?encounterId={r.Data!.Id}&isOffsite={IsOffsite}");
        });
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    // ── Helpers ───────────────────────────────────────────────────────
    private Encounter BuildEncounter()
    {
        var selectedCats = PoeCategories
            .Where(c => c.IsSelected)
            .Select(c => c.Name)
            .ToList();

        Guid.TryParse(EncounterId, out var existingId);
        Guid.TryParse(BookingId,   out var bookingGuid);

        return new Encounter
        {
            Id              = existingId == Guid.Empty ? Guid.NewGuid() : existingId,
            BookingId       = bookingGuid == Guid.Empty ? null : bookingGuid,
            EncounterType   = EncounterType,
            SessionId       = _bookingSessionId,
            CubicleId       = SelectedCubicleAssignment?.CubicleId ?? _pendingCubicleId,
            SupervisorId    = SelectedOnsiteSupervisor?.UserId ?? _pendingSupervisorId,
            // Section 1
            PatientFileNumber     = PatientFileNumber,
            PatientName           = PatientName,
            Dob                   = Dob,
            Gender                = Gender,
            ExamType              = ExamType,
            EncounterTime         = EncounterTime,
            Location              = Location,
            ReasonForVisit        = ReasonForVisit,
            History               = History,
            OcularHistory         = OcularHistory,
            OffSiteSupervisorName = OffSiteSupervisorName,
            OffSiteOpNumber       = OffSiteOpNumber,
            // Section 2
            DistanceVaOD     = DistanceVaOD,
            DistanceVaOS     = DistanceVaOS,
            NearVaOD         = NearVaOD,
            NearVaOS         = NearVaOS,
            RefractionOD     = RefractionOD,
            RefractionOS     = RefractionOS,
            NpcBreak         = NpcBreak,
            NpcRecovery      = NpcRecovery,
            CoverTestDistance= CoverTestDistance,
            CoverTestNear    = CoverTestNear,
            AnteriorSegment  = AnteriorSegment,
            PosteriorSegment = PosteriorSegment,
            AdditionalFindings = AdditionalFindings,
            // Section 3
            Diagnosis             = Diagnosis,
            DifferentialDiagnosis = DifferentialDiagnosis,
            ManagementPlan        = ManagementPlan,
            Referral              = Referral,
            FollowUp              = FollowUp,
            TreatmentNotes        = TreatmentNotes,
            // Section 4
            StudentReflection = StudentReflection,
            PoeCategoriesJson = JsonSerializer.Serialize(selectedCats),
            ConfirmAccurate   = ConfirmAccurate,
        };
    }

    private void PopulateFromEncounter(Encounter e)
    {
        EncounterType         = e.EncounterType;
        _bookingSessionId     = e.SessionId;
        _pendingCubicleId     = e.CubicleId;
        _pendingSupervisorId  = e.SupervisorId;
        PatientFileNumber     = e.PatientFileNumber;
        PatientName           = e.PatientName;
        Dob                   = e.Dob;
        Gender                = e.Gender;
        ExamType              = e.ExamType;
        EncounterTime         = e.EncounterTime;
        Location              = e.Location;
        ReasonForVisit        = e.ReasonForVisit;
        History               = e.History;
        OcularHistory         = e.OcularHistory;
        OffSiteSupervisorName = e.OffSiteSupervisorName;
        OffSiteOpNumber       = e.OffSiteOpNumber;
        DistanceVaOD          = e.DistanceVaOD;
        DistanceVaOS          = e.DistanceVaOS;
        NearVaOD              = e.NearVaOD;
        NearVaOS              = e.NearVaOS;
        RefractionOD          = e.RefractionOD;
        RefractionOS          = e.RefractionOS;
        NpcBreak              = e.NpcBreak;
        NpcRecovery           = e.NpcRecovery;
        CoverTestDistance     = e.CoverTestDistance;
        CoverTestNear         = e.CoverTestNear;
        AnteriorSegment       = e.AnteriorSegment;
        PosteriorSegment      = e.PosteriorSegment;
        AdditionalFindings    = e.AdditionalFindings;
        Diagnosis             = e.Diagnosis;
        DifferentialDiagnosis = e.DifferentialDiagnosis;
        ManagementPlan        = e.ManagementPlan;
        Referral              = e.Referral;
        FollowUp              = e.FollowUp;
        TreatmentNotes        = e.TreatmentNotes;
        StudentReflection     = e.StudentReflection;
        ConfirmAccurate       = e.ConfirmAccurate;
        if (e.CubicleId.HasValue && !string.IsNullOrWhiteSpace(e.CubicleNumber))
            Location = e.CubicleNumber;

        // Restore category selections
        try
        {
            var selected = JsonSerializer.Deserialize<List<string>>(e.PoeCategoriesJson) ?? new();
            foreach (var cat in PoeCategories)
                cat.IsSelected = selected.Contains(cat.Name);
        }
        catch { /* malformed JSON — ignore */ }
    }
}

// ══════════════════════════════════════════════════════════════
//  8. EncounterSubmitSuccessViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
[QueryProperty(nameof(IsOffsiteStr),"isOffsite")]
public partial class EncounterSubmitSuccessViewModel : BaseViewModel
{
    [ObservableProperty] private string _encounterId  = string.Empty;
    [ObservableProperty] private string _isOffsiteStr = string.Empty;

    public bool IsOffsite => IsOffsiteStr.Equals("True", StringComparison.OrdinalIgnoreCase);

    public string StatusMessage =>
        IsOffsite
            ? "✅ Auto-approved — no supervisor review needed."
            : "⏳ Submitted for supervisor review.";

    public string StatusColor =>
        IsOffsite ? "#15803D" : "#92400E";

    public string StatusBackground =>
        IsOffsite ? "#DCFCE7" : "#FEF3C7";

    public EncounterSubmitSuccessViewModel() { Title = "Encounter Submitted"; }

    [RelayCommand]
    private async Task ViewEncountersAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentEncounters}");

    [RelayCommand]
    private async Task StartAnotherAsync()
        => await Shell.Current.GoToAsync(AppRoutes.EncounterTypeSelect);

    [RelayCommand]
    private async Task GoToDashboardAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.StudentDashboard}");
}

// ══════════════════════════════════════════════════════════════
//  9. StudentEncounterDetailViewModel
// ══════════════════════════════════════════════════════════════
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class StudentEncounterDetailViewModel : BaseViewModel
{
    private readonly IEncounterService _encounters;

    [ObservableProperty] private string     _encounterId = string.Empty;
    [ObservableProperty] private Encounter? _encounter;
    [ObservableProperty] private bool       _canEdit;
    [ObservableProperty] private bool       _isApproved;
    [ObservableProperty] private bool       _needsRevision;

    public StudentEncounterDetailViewModel(IEncounterService encounters)
    { _encounters = encounters; Title = "Encounter Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(EncounterId, out var id)) return;
            var r = await _encounters.GetEncounterByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Encounter      = r.Data;
            CanEdit        = Encounter?.CanEdit ?? false;
            IsApproved     = Encounter?.IsApproved ?? false;
            NeedsRevision  = Encounter?.NeedsRevision ?? false;
            Title          = $"Encounter — {Encounter?.DateDisplay}";
        });
    }

    [RelayCommand]
    private async Task EditAndResubmitAsync()
    {
        if (Encounter is null) return;
        // Navigate to EncounterForm pre-loaded with this encounter's data
        await Shell.Current.GoToAsync(
            $"{AppRoutes.EncounterForm}?encounterId={Encounter.Id}" +
            $"&encounterType={Encounter.EncounterType}");
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ══════════════════════════════════════════════════════════════
//  10. StudentPoeViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentPoeViewModel : BaseViewModel
{
    private readonly IPoeService _poe;

    [ObservableProperty] private PoeSummary _summary = new();
    [ObservableProperty] private ObservableCollection<RecentPoeEncounter> _recentEntries = new();
    [ObservableProperty] private bool _isEmpty;

    public StudentPoeViewModel(IPoeService poe)
    { _poe = poe; Title = "PoE Progress"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var sr = await _poe.GetPoeSummaryAsync();
            if (!sr.Success) { SetError(sr.Error!); return; }
            Summary  = sr.Data ?? new();

            var rr = await _poe.GetRecentEntriesAsync(10);
            if (rr.Success)
            {
                RecentEntries = new(rr.Data ?? new());
                IsEmpty       = !RecentEntries.Any();
            }
        });
    }
}

// ══════════════════════════════════════════════════════════════
//  11. StudentNotificationsViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentNotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _svc;

    [ObservableProperty] private ObservableCollection<AppNotification> _items = new();
    [ObservableProperty] private bool _isEmpty;

    public StudentNotificationsViewModel(INotificationService svc)
    { _svc = svc; Title = "Notifications"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _svc.GetMyNotificationsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            Items   = new(r.Data ?? new());
            IsEmpty = !Items.Any();
        });
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await _svc.MarkAllReadAsync();
        foreach (var n in Items) n.IsRead = true;
        OnPropertyChanged(nameof(Items));
    }
}

// ══════════════════════════════════════════════════════════════
//  12. StudentProfileViewModel
// ══════════════════════════════════════════════════════════════
public partial class StudentProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profile;
    private readonly IAuthService    _auth;

    [ObservableProperty] private UserProfile? _userProfile;
    [ObservableProperty] private string       _fullName        = string.Empty;
    [ObservableProperty] private string       _phone           = string.Empty;
    [ObservableProperty] private bool         _saved;
    [ObservableProperty] private string       _newPassword     = string.Empty;
    [ObservableProperty] private string       _confirmPassword = string.Empty;
    [ObservableProperty] private bool         _passwordChanged;

    public StudentProfileViewModel(IProfileService profile, IAuthService auth)
    { _profile = profile; _auth = auth; Title = "My Profile"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _profile.GetProfileAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            UserProfile = r.Data;
            FullName    = r.Data!.FullName;
            Phone       = r.Data.Phone;
        });
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName)) { SetError("Full name is required."); return; }
        await RunBusyAsync(async () =>
        {
            var r = await _profile.UpdateProfileAsync(FullName.Trim(), Phone.Trim());
            if (!r.Success) { SetError(r.Error!); return; }
            Saved = true;
        });
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (NewPassword.Length < 8) { SetError("Password must be at least 8 characters."); return; }
        if (NewPassword != ConfirmPassword) { SetError("Passwords do not match."); return; }
        await RunBusyAsync(async () =>
        {
            var r = await _auth.ChangePasswordAsync(NewPassword);
            if (!r.Success) { SetError(r.Error!); return; }
            PasswordChanged = true; NewPassword = string.Empty; ConfirmPassword = string.Empty;
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    { await _auth.SignOutAsync(); App.RouteToLogin(); }
}
