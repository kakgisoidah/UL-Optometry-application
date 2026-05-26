namespace UL_Optometry.Views.Patient.Booking;

using UL_Optometry.ViewModels.Patient;

public partial class MyBookingsPage : ContentPage
{
    private const double TabWidth = 120.0;
    public MyBookingsPage(MyBookingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as MyBookingsViewModel)?.LoadCommand.Execute(null);
    }

    private void OnUpcomingTab(object sender, EventArgs e)
    {
        SwitchTab(0);

    }

    private void OnPastTab(object sender, EventArgs e)
    {
        SwitchTab(1);
    }

    private void OnCancelledTab(object sender, EventArgs e)
    {
        SwitchTab(2);
    }

    // ── Visual tab switching ──────────────────────────────────────────
    private void SwitchTab(int i)
    {
        // Show only the active panel
        UpcomingPanel.IsVisible = i == 0;
        PastPanel.IsVisible = i == 1;
        CancelledPanel.IsVisible = i == 2;

        // Slide the underline indicator
        TabIndicator.TranslationX = i * TabWidth;

        // Style active/inactive tab buttons
        void Style(Button b, bool active)
        {
            b.TextColor = active
       ? (Color)Application.Current.Resources["Primary"]
       : (Color)Application.Current.Resources["Muted"];
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        Style(UpcomingTab, i == 0);
        Style(PastTab, i == 1);
        Style(CancelledTab, i == 2);
    }
}