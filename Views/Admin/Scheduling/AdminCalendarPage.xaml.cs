namespace UL_Optometry.Views.Admin.Scheduling;
using UL_Optometry.ViewModels.Admin;

public partial class AdminCalendarPage : ContentPage
{
	public AdminCalendarPage(AdminCalendarViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as AdminCalendarViewModel)?.LoadCommand.Execute(null);
    }
}