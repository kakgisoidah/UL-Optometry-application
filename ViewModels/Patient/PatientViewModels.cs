// ════════════════════════════════════════════════════════════════════════
//  ViewModels/Patient/PatientViewModels.cs
//  All 15 Patient portal ViewModels.
// ════════════════════════════════════════════════════════════════════════

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UL_Optometry.Constants;
using UL_Optometry.Models;
using UL_Optometry.Models.Admin;
using UL_Optometry.Models.Auth;
using UL_Optometry.Models.Notification;
using UL_Optometry.Services.Interfaces;
using UL_Optometry.ViewModels.Base;

namespace UL_Optometry.ViewModels.Patient;
// ── 1. PatientDashboardViewModel ─────────────────────────────────────────
public partial class PatientDashboardViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    private readonly INotificationService _notifications;
    private readonly IAuthService _auth;

    [ObservableProperty] private Booking? _nextBooking;
    [ObservableProperty] private bool _hasNextBooking;
    [ObservableProperty] private int _unreadCount;
    [ObservableProperty] private string _greeting = string.Empty;

    public PatientDashboardViewModel(
        IBookingService bookings, INotificationService notifications, IAuthService auth)
    {
        _bookings = bookings; _notifications = notifications; _auth = auth;
        Title = "Home";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var name = _auth.CurrentUser?.FullName.Split(' ')[0] ?? "there";
            Greeting = $"Hello, {name} 👋";

            var br = await _bookings.GetMyBookingsAsync();
            if (br.Success)
            {
                NextBooking = br.Data?
                    .Where(b => b.BookingStatus is BookingStatus.Pending or BookingStatus.Accepted
                             && b.Date >= DateTime.UtcNow.Date)
                    .OrderBy(b => b.Date).FirstOrDefault();
                HasNextBooking = NextBooking is not null;
            }

            var nr = await _notifications.GetMyNotificationsAsync();
            if (nr.Success) UnreadCount = nr.Data?.Count(n => !n.IsRead) ?? 0;
        });
    }

    [RelayCommand]
    private async Task GoToBookAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.SelectClinic}");
    [RelayCommand]
    private async Task GoToBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.MyBookings}");
    [RelayCommand]
    private async Task GoToHistoryAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.VisitHistory}");
    [RelayCommand]
    private async Task GoToProfileAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.PatientProfile}");

    public string PatientName => _auth.CurrentUser?.FullName ?? string.Empty;
    [RelayCommand]
    private async Task GoToNotificationsAsync()
        => await Shell.Current.GoToAsync(AppRoutes.PatientNotifications);
    [RelayCommand]
    private async Task ViewNextBookingAsync()
    {
        if (NextBooking is null) return;
        await Shell.Current.GoToAsync($"{AppRoutes.BookingDetail}?bookingId={NextBooking.Id}");
    }
}

// ── 2. SelectClinicViewModel ─────────────────────────────────────────────
public partial class SelectClinicViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private ObservableCollection<Clinic> _clinics = new();
    [ObservableProperty] private Clinic? _selectedClinic;

    public SelectClinicViewModel(IBookingService bookings)
    { _bookings = bookings; Title = "Select Clinic"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.GetClinicsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            Clinics = new ObservableCollection<Clinic>(r.Data!);
        });
    }

    [RelayCommand]
    private void SelectClinic(Clinic clinic)
    {
        // Deselect all others
        foreach (var c in Clinics) c.IsSelected = false;
        // Select tapped clinic
        clinic.IsSelected = true;
        SelectedClinic = clinic;
        // Trigger UI refresh
        Clinics = new ObservableCollection<Clinic>(Clinics);
    }

    [RelayCommand]
    private async Task ProceedAsync()
    {
        if (SelectedClinic is null) { SetError("Please select a clinic."); return; }
        await Shell.Current.GoToAsync(
            $"{AppRoutes.SelectDate}" +
            $"?clinicId={SelectedClinic.Id}" +
            $"&weekday={SelectedClinic.Weekday}" +
            $"&clinicName={Uri.EscapeDataString(SelectedClinic.Name)}" +
            $"&hexColor={Uri.EscapeDataString(SelectedClinic.HexColor)}");
    }
}

// ── 3. SelectDateViewModel ───────────────────────────────────────────────
[QueryProperty(nameof(ClinicId), "clinicId")]
[QueryProperty(nameof(Weekday), "weekday")]
[QueryProperty(nameof(ClinicName), "clinicName")]
[QueryProperty(nameof(HexColor), "hexColor")]
public partial class SelectDateViewModel : BaseViewModel
{
    [ObservableProperty] private int _clinicId;
    [ObservableProperty] private int _weekday;
    [ObservableProperty] private string _clinicName = string.Empty;
    [ObservableProperty] private string _hexColor = "#1E3A8A";
    [ObservableProperty] private ObservableCollection<CalendarDay> _calendarDays = new();
    [ObservableProperty] private DateTime _currentMonth = DateTime.Today;
    [ObservableProperty] private CalendarDay? _selectedDay;
    [ObservableProperty] private string _monthLabel = string.Empty;
    public bool HasSelectedDay => SelectedDay is not null;
    public double ContinueOpacity => SelectedDay is not null ? 1.0 : 0.4;

    private readonly IBookingService _bookingService;
    private readonly ISchedulingService _schedulingService;
    private List<DateTime> _blockedDates = new();

    public SelectDateViewModel(IBookingService bookingService, ISchedulingService schedulingService)
    {
        _bookingService = bookingService;
        _schedulingService = schedulingService;
        Title = "Select Date";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Load blocked dates from DB so patient calendar greys them out
        try
        {
            var r = await _schedulingService.GetBlockedDatesAsync();
            if (r.Success) _blockedDates = r.Data?.Select(b => b.Date).ToList() ?? new();
        }
        catch { /* if fetch fails, just show all days */ }
        BuildCalendar();
    }
    [RelayCommand] private void PreviousMonth() { CurrentMonth = CurrentMonth.AddMonths(-1); BuildCalendar(); }
    [RelayCommand] private void NextMonth() { CurrentMonth = CurrentMonth.AddMonths(1); BuildCalendar(); }

    [RelayCommand]
    private void SelectDay(CalendarDay day)
    {
        if (!day.IsSelectable) return;
        if (SelectedDay is not null) SelectedDay.IsSelected = false;
        day.IsSelected = true;
        SelectedDay = day;
        OnPropertyChanged(nameof(HasSelectedDay));
        OnPropertyChanged(nameof(ContinueOpacity));
    }

    [RelayCommand]
    private async Task ProceedAsync()
    {
        if (SelectedDay is null) { SetError("Please select a date."); return; }
        var date = SelectedDay.Date.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync($"{AppRoutes.SelectSlot}?clinicId={ClinicId}&date={date}");
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    private void BuildCalendar()
    {
        MonthLabel = CurrentMonth.ToString("MMMM yyyy");
        var days = new ObservableCollection<CalendarDay>();
        var first = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        int startDow = ((int)first.DayOfWeek + 6) % 7;
        for (int i = 0; i < startDow; i++) days.Add(new CalendarDay { IsEmpty = true });
        for (var d = first; d <= last; d = d.AddDays(1))
        {
            int dow = ((int)d.DayOfWeek + 6) % 7;
            bool matchesClinic = (dow + 1) == Weekday;
            bool isBlocked = _blockedDates.Any(b => b.Date == d.Date);
            days.Add(new CalendarDay
            {
                Date = d,
                IsSelectable = matchesClinic && d.Date >= DateTime.Today && !isBlocked,
                IsClinicDay = matchesClinic,
                IsPast = d.Date < DateTime.Today,
                IsBlocked = isBlocked,
            });
        }
        CalendarDays = days;
    }
}

public partial class CalendarDay : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public DateTime Date { get; init; }
    public bool IsEmpty { get; init; }
    public bool IsSelectable { get; init; }
    public bool IsClinicDay { get; init; }
    public bool IsPast { get; init; }
    public bool IsBlocked { get; init; }
    public string DayNumber => IsEmpty ? string.Empty : Date.Day.ToString();
}

// ── 4. SelectSlotViewModel ───────────────────────────────────────────────
[QueryProperty(nameof(ClinicId), "clinicId")]
[QueryProperty(nameof(DateStr), "date")]
[QueryProperty(nameof(ClinicName), "clinicName")]
public partial class SelectSlotViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private int _clinicId;
    [ObservableProperty] private string _dateStr = string.Empty;
    [ObservableProperty] private string _clinicName = string.Empty;
    [ObservableProperty] private ObservableCollection<Session> _slots = new();
    [ObservableProperty] private Session? _selectedSlot;

    public string DateDisplay =>
        DateTime.TryParse(DateStr, out var d) ? d.ToString("dd MMMM yyyy") : DateStr;

    public SelectSlotViewModel(IBookingService bookings) { _bookings = bookings; Title = "Select Time"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!DateTime.TryParse(DateStr, out var date)) return;
            var r = await _bookings.GetAvailableSlotsAsync(date, ClinicId);
            if (!r.Success) { SetError(r.Error!); return; }
            Slots = new ObservableCollection<Session>(r.Data!);
        });
    }

    [RelayCommand] private void SelectSlot(Session slot) { SelectedSlot = slot; ClearError(); }

    [RelayCommand]
    private async Task ProceedAsync()
    {
        if (SelectedSlot is null) { SetError("Please select a time slot."); return; }
        await Shell.Current.GoToAsync(
            $"{AppRoutes.ConfirmBooking}?clinicId={ClinicId}&date={DateStr}&sessionId={SelectedSlot.Id}");
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ── 5. ConfirmBookingViewModel ───────────────────────────────────────────
[QueryProperty(nameof(ClinicId), "clinicId")]
[QueryProperty(nameof(DateStr), "date")]
[QueryProperty(nameof(SessionId), "sessionId")]
public partial class ConfirmBookingViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private int _clinicId;
    [ObservableProperty] private string _dateStr = string.Empty;
    [ObservableProperty] private int _sessionId;
    [ObservableProperty] private string _clinicName = string.Empty;
    [ObservableProperty] private string _dateDisplay = string.Empty;
    [ObservableProperty] private string _slotDisplay = string.Empty;

    public ConfirmBookingViewModel(IBookingService bookings) { _bookings = bookings; Title = "Confirm Booking"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (DateTime.TryParse(DateStr, out var date))
                DateDisplay = date.ToString("dd MMMM yyyy");
            var cr = await _bookings.GetClinicsAsync();
            if (cr.Success) ClinicName = cr.Data?.FirstOrDefault(c => c.Id == ClinicId)?.Name ?? string.Empty;
            var sr = await _bookings.GetAvailableSlotsAsync(DateTime.Parse(DateStr), ClinicId);
            if (sr.Success) SlotDisplay = sr.Data?.FirstOrDefault(s => s.Id == SessionId)?.Display ?? string.Empty;
        });
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.CreateBookingAsync(new CreateBookingRequest
            { ClinicId = ClinicId, SessionId = SessionId, Date = DateTime.Parse(DateStr) });
            if (!r.Success) { SetError(r.Error!); return; }
            await Shell.Current.GoToAsync($"{AppRoutes.BookingSuccess}?bookingId={r.Data!.Id}");
        });
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ── 6. BookingSuccessViewModel ───────────────────────────────────────────
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class BookingSuccessViewModel : BaseViewModel
{
    [ObservableProperty] private string _bookingId = string.Empty;
    public BookingSuccessViewModel() { Title = "Booking Confirmed"; }

    [RelayCommand]
    private async Task ViewBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.MyBookings}");
    [RelayCommand]
    private async Task BookAnotherAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.SelectClinic}");
    [RelayCommand]
    private async Task GoHomeAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.PatientDashboard}");
}

// ── 7. MyBookingsViewModel ───────────────────────────────────────────────
public partial class MyBookingsViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private ObservableCollection<Booking> _upcoming = new();
    [ObservableProperty] private ObservableCollection<Booking> _past = new();
    [ObservableProperty] private ObservableCollection<Booking> _cancelled = new();
    [ObservableProperty] private bool _hasUpcoming, _hasPast, _hasCancelled;

    public MyBookingsViewModel(IBookingService bookings) { _bookings = bookings; Title = "My Bookings"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.GetMyBookingsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            var all = r.Data ?? new();
            Upcoming = new(all.Where(b => b.BookingStatus is BookingStatus.Pending
                or BookingStatus.Accepted or BookingStatus.InProgress).OrderBy(b => b.Date));
            Past = new(all.Where(b => b.BookingStatus == BookingStatus.Completed).OrderByDescending(b => b.Date));
            Cancelled = new(all.Where(b => b.BookingStatus == BookingStatus.Cancelled).OrderByDescending(b => b.Date));
            HasUpcoming = Upcoming.Any(); HasPast = Past.Any(); HasCancelled = Cancelled.Any();
        });
    }

    [RelayCommand]
    private async Task ViewBookingAsync(Booking b)
        => await Shell.Current.GoToAsync($"{AppRoutes.BookingDetail}?bookingId={b.Id}");
}

// ── 8. BookingDetailViewModel ────────────────────────────────────────────
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class BookingDetailViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private string _bookingId = string.Empty;
    [ObservableProperty] private Booking? _booking;
    [ObservableProperty] private bool _canCancel;

    public BookingDetailViewModel(IBookingService bookings) { _bookings = bookings; Title = "Booking Detail"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(BookingId, out var id)) return;
            var r = await _bookings.GetBookingByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Booking = r.Data; CanCancel = Booking?.CanCancel ?? false;
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (Booking is null) return;
        await Shell.Current.GoToAsync($"{AppRoutes.CancelBooking}?bookingId={Booking.Id}");
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ── 9. CancelBookingViewModel ────────────────────────────────────────────
[QueryProperty(nameof(BookingId), "bookingId")]
public partial class CancelBookingViewModel : BaseViewModel
{
    private readonly IBookingService _bookings;
    [ObservableProperty] private string _bookingId = string.Empty;
    [ObservableProperty] private string _selectedReason = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _cancelled;

    public List<string> Reasons { get; } = new()
    { "Schedule conflict","Health / illness","Transport issues","No longer needed","Wrong clinic selected","Other" };

    public CancelBookingViewModel(IBookingService bookings) { _bookings = bookings; Title = "Cancel Booking"; }

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedReason)) { SetError("Please select a reason."); return; }
        if (!Guid.TryParse(BookingId, out var id)) return;
        await RunBusyAsync(async () =>
        {
            var r = await _bookings.CancelBookingAsync(new CancelBookingRequest
            { BookingId = id, Reason = SelectedReason, Notes = Notes });
            if (!r.Success) { SetError(r.Error!); return; }
            Cancelled = true;
        });
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
    [RelayCommand]
    private async Task GoToBookingsAsync()
        => await Shell.Current.GoToAsync($"//{AppRoutes.MyBookings}");
}

// ── 10. VisitHistoryViewModel ────────────────────────────────────────────
public partial class VisitHistoryViewModel : BaseViewModel
{
    private readonly IPatientEncounterService _svc;
    [ObservableProperty] private ObservableCollection<Encounter> _encounters = new();
    [ObservableProperty] private bool _isEmpty;

    public VisitHistoryViewModel(IPatientEncounterService svc) { _svc = svc; Title = "Visit History"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _svc.GetMyEncountersAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            Encounters = new(r.Data ?? new()); IsEmpty = !Encounters.Any();
        });
    }

    [RelayCommand]
    private async Task ViewEncounterAsync(Encounter e)
        => await Shell.Current.GoToAsync($"{AppRoutes.PatientEncounterDetail}?encounterId={e.Id}");
}

// ── 11. PatientEncounterDetailViewModel ─────────────────────────────────
[QueryProperty(nameof(EncounterId), "encounterId")]
public partial class PatientEncounterDetailViewModel : BaseViewModel
{
    private readonly IPatientEncounterService _svc;
    [ObservableProperty] private string _encounterId = string.Empty;
    [ObservableProperty] private Encounter? _encounter;

    public PatientEncounterDetailViewModel(IPatientEncounterService svc) { _svc = svc; Title = "Visit Details"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!Guid.TryParse(EncounterId, out var id)) return;
            var r = await _svc.GetEncounterByIdAsync(id);
            if (!r.Success) { SetError(r.Error!); return; }
            Encounter = r.Data;
        });
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ── 12. PatientProfileViewModel ──────────────────────────────────────────
public partial class PatientProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profile;
    private readonly IAuthService _auth;
    private readonly INotificationService _notifications;
    [ObservableProperty] private UserProfile? _userProfile;
    [ObservableProperty] private int _unreadCount;

    public PatientProfileViewModel(IProfileService p, IAuthService a, INotificationService n)
    { _profile = p; _auth = a; _notifications = n; Title = "My Profile"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _profile.GetProfileAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            UserProfile = r.Data;
            var nr = await _notifications.GetMyNotificationsAsync();
            if (nr.Success) UnreadCount = nr.Data?.Count(n => !n.IsRead) ?? 0;
            OnPropertyChanged(nameof(Initials));
            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(Email));
        });
    }

    // Passthrough properties so XAML can bind directly
    public string Initials  => UserProfile?.Initials  ?? "?";
    public string FullName  => UserProfile?.FullName  ?? string.Empty;
    public string Email     => UserProfile?.Email     ?? string.Empty;

    [RelayCommand]
    private async Task EditProfileAsync()
        => await Shell.Current.GoToAsync(AppRoutes.EditPatientProfile);
    [RelayCommand]
    private async Task ViewNotificationsAsync()
        => await Shell.Current.GoToAsync(AppRoutes.PatientNotifications);
    [RelayCommand]
    private async Task HelpAsync()
        => await Shell.Current.GoToAsync(AppRoutes.Help);
    [RelayCommand]
    private async Task PrivacyPolicyAsync()
        => await Shell.Current.GoToAsync(AppRoutes.PrivacyPolicy);
    [RelayCommand]
    private async Task LogoutAsync()
    { await _auth.SignOutAsync(); App.RouteToLogin(); }
}

// ── 13. EditPatientProfileViewModel ─────────────────────────────────────
public partial class EditPatientProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profile;
    private readonly IAuthService _auth;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private bool _saved;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool _passwordChanged;

    public EditPatientProfileViewModel(IProfileService p, IAuthService a)
    { _profile = p; _auth = a; Title = "Edit Profile"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _profile.GetProfileAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            FullName = r.Data!.FullName; Phone = r.Data.Phone; Email = r.Data.Email;
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

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

// ── 14. PatientNotificationsViewModel ───────────────────────────────────
public partial class PatientNotificationsViewModel : BaseViewModel
{
    private readonly INotificationService _svc;
    [ObservableProperty] private ObservableCollection<AppNotification> _items = new();
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private int _unreadCount;

    // Alias used by XAML
    public ObservableCollection<AppNotification> Notifications => Items;

    public PatientNotificationsViewModel(INotificationService svc) { _svc = svc; Title = "Notifications"; }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            var r = await _svc.GetMyNotificationsAsync();
            if (!r.Success) { SetError(r.Error!); return; }
            Items = new(r.Data ?? new()); IsEmpty = !Items.Any();
            UnreadCount = Items.Count(n => !n.IsRead);
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

// ── 15. HelpViewModel ───────────────────────────────────────────────────
public partial class HelpViewModel : BaseViewModel
{
    public List<FaqItem> Faqs { get; } = new()
    {
        new("How do I book an appointment?",
            "Go to the Book tab, select the clinic type, choose an available date and time slot, then confirm."),
        new("Can I choose my student or cubicle?",
            "No. The coordinator assigns students and cubicles. You only select clinic type, date, and time."),
        new("How do I cancel a booking?",
            "Go to My Bookings, tap the booking, then tap Cancel. Only possible before a student accepts."),
        new("When can I see my visit results?",
            "Your summary appears in Visit History after the supervising optometrist signs off the encounter."),
        new("Is my information safe?",
            "Yes. Data is protected under POPIA (Act 4 of 2013) and stored in South Africa."),
        new("How do I update my contact details?",
            "Go to Profile → Edit Profile to update your name, phone number, or password."),
    };

    public HelpViewModel() { Title = "Help & Support"; }

    [RelayCommand]
    private async Task CallClinicAsync()
        => await Launcher.OpenAsync("tel:+27152682000");
    [RelayCommand]
    private async Task EmailClinicAsync()
        => await Launcher.OpenAsync("mailto:optometry@ul.ac.za");
    [RelayCommand]
    private async Task OpenMapsAsync()
        => await Launcher.OpenAsync("https://maps.google.com/?q=University+of+Limpopo+Optometry");
}

public partial class FaqItem : ObservableObject
{
    [ObservableProperty] private bool _isExpanded;
    public string Question { get; }
    public string Answer { get; }
    public FaqItem(string q, string a) { Question = q; Answer = a; }
    [RelayCommand] private void Toggle() => IsExpanded = !IsExpanded;
}
