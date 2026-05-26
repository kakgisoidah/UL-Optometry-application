namespace UL_Optometry.Views.Patient.Encounters;

using UL_Optometry.ViewModels.Patient;

public partial class PatientEncounterDetailPage : ContentPage
{
	public PatientEncounterDetailPage(PatientEncounterDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}