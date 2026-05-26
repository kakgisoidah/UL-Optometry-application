namespace UL_Optometry.Views.Admin.Bookings;
using UL_Optometry.ViewModels.Admin;

public partial class AdminBookingsPage : ContentPage
{
	public AdminBookingsPage(AdminBookingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}