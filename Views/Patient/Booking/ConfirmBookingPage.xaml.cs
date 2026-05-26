namespace UL_Optometry.Views.Patient.Booking;

using UL_Optometry.ViewModels.Patient;

public partial class ConfirmBookingPage : ContentPage
{
	public ConfirmBookingPage(ConfirmBookingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}