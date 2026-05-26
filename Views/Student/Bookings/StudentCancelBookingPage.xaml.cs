namespace UL_Optometry.Views.Student.Bookings;
using UL_Optometry.ViewModels.Student;

public partial class StudentCancelBookingPage : ContentPage
{
	public StudentCancelBookingPage(StudentCancelBookingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}