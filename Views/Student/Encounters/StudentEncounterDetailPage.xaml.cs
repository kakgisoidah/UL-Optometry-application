namespace UL_Optometry.Views.Student.Encounters;
using UL_Optometry.ViewModels.Student;
public partial class StudentEncounterDetailPage : ContentPage
{
	public StudentEncounterDetailPage(StudentEncounterDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}