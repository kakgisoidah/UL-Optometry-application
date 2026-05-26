namespace UL_Optometry.Views.Student.Profile;
using UL_Optometry.ViewModels.Student;

public partial class StudentProfilePage : ContentPage
{
	public StudentProfilePage(StudentProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}