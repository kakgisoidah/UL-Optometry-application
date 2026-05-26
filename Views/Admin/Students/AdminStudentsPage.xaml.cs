namespace UL_Optometry.Views.Admin.Students;
using UL_Optometry.ViewModels.Admin;
public partial class AdminStudentsPage : ContentPage
{
    const double W = 110.0;  // width per tab for indicator sliding
    public AdminStudentsPage(AdminStudentsViewModel vm  )
	{
		InitializeComponent();
		BindingContext = vm;
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as AdminStudentsViewModel)?.LoadCommand.Execute(null);
    }

    private void OnAllTab(object sender, EventArgs e)
    {
        Switch(0);
    }

    private void OnAtRiskTab(object sender, EventArgs e)
    {
        Switch(1);
    }

    private void OnInactTab(object sender, EventArgs e)
    {
        Switch(2);
    }
    private void Switch(int i)
    {
        // Show only the active panel
        AllSPanel.IsVisible = i == 0;
        RiskSPanel.IsVisible = i == 1;
        InactSPanel.IsVisible = i == 2;

        // Slide the underline indicator
        StudTabBar.TranslationX = i * W;

        // Style active/inactive tab buttons
        void S(Button b, bool active)
        {
            b.TextColor = active
                ? (Color)Application.Current.Resources["Primary"]
                : (Color)Application.Current.Resources["Muted"];
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        S(AllSTab, i == 0);
        S(RiskSTab, i == 1);
        S(InactSTab, i == 2);
    }
}