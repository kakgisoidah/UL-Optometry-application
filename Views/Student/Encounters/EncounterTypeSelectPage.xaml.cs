namespace UL_Optometry.Views.Student.Encounters;
using UL_Optometry.ViewModels.Student;
public partial class EncounterTypeSelectPage : ContentPage
{
	public EncounterTypeSelectPage(EncounterTypeSelectViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}