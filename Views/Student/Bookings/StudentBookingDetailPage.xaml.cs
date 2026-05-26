namespace UL_Optometry.Views.Student.Bookings;
using UL_Optometry.ViewModels.Student;

public partial class StudentBookingDetailPage : ContentPage
{
	public StudentBookingDetailPage(StudentBookingDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}