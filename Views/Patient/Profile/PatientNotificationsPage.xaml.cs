namespace UL_Optometry.Views.Patient.Profile;
using UL_Optometry.ViewModels.Patient;

public partial class PatientNotificationsPage : ContentPage
{
	public PatientNotificationsPage(PatientNotificationsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}