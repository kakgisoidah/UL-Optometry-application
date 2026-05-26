namespace UL_Optometry.Views.Patient.Dashboard;

using UL_Optometry.ViewModels.Patient;
public partial class PatientDashboardPage : ContentPage
{
	public PatientDashboardPage(PatientDashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}