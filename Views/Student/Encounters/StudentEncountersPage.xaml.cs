namespace UL_Optometry.Views.Student.Encounters;
using UL_Optometry.ViewModels.Student;
public partial class StudentEncountersPage : ContentPage
{
    const double W = 70.0;  // width per tab for indicator sliding

    public StudentEncountersPage(StudentEncountersViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as StudentEncountersViewModel)?.LoadCommand.Execute(null);
    }

    private void OnAllTab(object sender, EventArgs e)
    {
        Switch(0);
    }

    private void OnDraftTab(object sender, EventArgs e)
    {
        Switch(1);

    }

    private void OnSubTab(object sender, EventArgs e)
    {
        Switch(2);
    }

    private void OnAppTab(object sender, EventArgs e)
    {
        Switch(3);
    }

    private void OnRevTab(object sender, EventArgs e)
    {
        Switch(4);
    }

    private void Switch(int i)
    {
        // Show only the active panel
        AllEncPanel.IsVisible = i == 0;
        DraftEncPanel.IsVisible = i == 1;
        SubEncPanel.IsVisible = i == 2;
        AppEncPanel.IsVisible = i == 3;
        RevEncPanel.IsVisible = i == 4;

        // Slide the underline indicator
        EncTabBar.TranslationX = i * W;

        // Style active/inactive tab buttons
        void S(Button b, bool active)
        {
            b.TextColor = active
       ? (Color)Application.Current.Resources["Primary"]
       : (Color)Application.Current.Resources["Muted"];
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        S(AllTab, i == 0);
        S(DraftTab, i == 1);
        S(SubTab, i == 2);
        S(AppTab, i == 3);
        S(RevTab, i == 4);
    }
}