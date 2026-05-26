namespace UL_Optometry.Views.Patient.Booking;
using UL_Optometry.ViewModels.Patient;

public partial class BookingSuccessPage : ContentPage
{
	public BookingSuccessPage(BookingSuccessViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}