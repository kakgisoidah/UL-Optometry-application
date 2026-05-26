namespace UL_Optometry.Views.Student.PoE;
using UL_Optometry.ViewModels.Student;

public partial class StudentPoePage : ContentPage
{
	public StudentPoePage(StudentPoeViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}