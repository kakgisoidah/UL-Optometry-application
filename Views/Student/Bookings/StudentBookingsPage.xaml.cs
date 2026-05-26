namespace UL_Optometry.Views.Student.Bookings;
using UL_Optometry.ViewModels.Student;
public partial class StudentBookingsPage : ContentPage
{
    const double W = 90.0;  // width per tab for indicator sliding
    public StudentBookingsPage(StudentBookingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	
	}
	protected override void OnAppearing()
	{
		base.OnAppearing();
		(BindingContext as StudentBookingsViewModel)?.LoadCommand.Execute(null);
    }

    private void OnQueueTab(object sender, EventArgs e)
    {
        Switch(0);
    }

    private void OnAcceptedTab(object sender, EventArgs e)
    {
        Switch(1);
    }

    private void OnInProgTab(object sender, EventArgs e)
    {
        Switch(2);
    }

    private void OnCompTab(object sender, EventArgs e)
    {
        Switch(3);
    }

    private void Switch(int i)
    {
        // Show only the active panel
        QueuePanel.IsVisible = i == 0;
        AcceptedPanel.IsVisible = i == 1;
        InProgPanel.IsVisible = i == 2;
        CompPanel.IsVisible = i == 3;

        // Slide the underline indicator
        TabBar.TranslationX = i * W;

        // Style active/inactive tab buttons
        void S(Button b, bool active)
        {
            b.TextColor = active
                ? (Color)Application.Current.Resources["Primary"]
                : (Color)Application.Current.Resources["Muted"];
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        S(QueueTab, i == 0);
        S(AcceptedTab, i == 1);
        S(InProgTab, i == 2);
        S(CompTab, i == 3);
    }
}