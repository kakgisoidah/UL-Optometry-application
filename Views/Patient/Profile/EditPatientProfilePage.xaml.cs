namespace UL_Optometry.Views.Patient.Profile;

using UL_Optometry.ViewModels.Patient;

public partial class EditPatientProfilePage : ContentPage
{
	public EditPatientProfilePage(EditPatientProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}