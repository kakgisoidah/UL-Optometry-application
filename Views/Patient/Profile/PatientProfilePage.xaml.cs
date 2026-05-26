namespace UL_Optometry.Views.Patient.Profile;

using UL_Optometry.Models.Admin;
using UL_Optometry.ViewModels.Patient;

public partial class PatientProfilePage : ContentPage
{
	public PatientProfilePage(PatientProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}