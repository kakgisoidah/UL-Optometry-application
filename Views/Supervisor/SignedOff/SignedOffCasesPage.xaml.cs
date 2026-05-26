namespace UL_Optometry.Views.Supervisor.SignedOff;

using UL_Optometry.ViewModels.Supervisor;

public partial class SignedOffCasesPage : ContentPage
{
    private const double TabWidth = 120.0;
    public SignedOffCasesPage(SignedOffCasesViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }

    private void OnAllTab(object sender, EventArgs e)
    {
        SwitchTab(0);
    }

    private void OnWeekTab(object sender, EventArgs e)
    {
                SwitchTab(1);
    }

    private void OnMonthTab(object sender, EventArgs e)
    {
        SwitchTab(2);
    }

    private void SwitchTab(int i)
    {
        // Show only the active panel
        AllPanel.IsVisible = i == 0;
        WeekPanel.IsVisible = i == 1;
        MonthPanel.IsVisible = i == 2;

        // Slide the underline indicator
        TabIndicator.TranslationX = i * TabWidth;

        // Style active/inactive tab buttons
        void S(Button b, bool active)
        {
            b.TextColor = active
         ? (Color)Application.Current.Resources["Primary"]
         : (Color)Application.Current.Resources["Muted"];
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        S(AllTab, i == 0);
        S(WeekTab, i == 1);
        S(MonthTab, i == 2);
    }
}