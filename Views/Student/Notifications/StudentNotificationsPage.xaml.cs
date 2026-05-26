namespace UL_Optometry.Views.Student.Notifications;
using UL_Optometry.ViewModels.Student;

public partial class StudentNotificationsPage : ContentPage
{
	public StudentNotificationsPage(StudentNotificationsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}