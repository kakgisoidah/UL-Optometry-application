namespace UL_Optometry.Views.Student.Dashboard;
using UL_Optometry.ViewModels.Student;

public partial class StudentDashboardPage : ContentPage
{
	public StudentDashboardPage(StudentDashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}