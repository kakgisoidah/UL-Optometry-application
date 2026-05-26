namespace UL_Optometry.Views.Patient.Booking;
using UL_Optometry.ViewModels.Patient;

public partial class CancelBookingPage : ContentPage
{
	public CancelBookingPage(CancelBookingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}