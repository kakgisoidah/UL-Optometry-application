namespace UL_Optometry.Views.Student.Encounters;

using UL_Optometry.ViewModels.Student;

public partial class EncounterSubmitSuccessPage : ContentPage
{
	public EncounterSubmitSuccessPage(EncounterSubmitSuccessViewModel v)
	{
		InitializeComponent();
		BindingContext = v;
	}
}