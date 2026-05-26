namespace UL_Optometry.Views.Patient.Booking;
using UL_Optometry.ViewModels.Patient;

public partial class BookingDetailPage : ContentPage
{
	public BookingDetailPage(BookingDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}