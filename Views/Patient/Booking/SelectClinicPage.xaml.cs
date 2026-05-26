namespace UL_Optometry.Views.Patient.Booking;

using UL_Optometry.ViewModels.Patient;


public partial class SelectClinicPage : ContentPage
{
	public SelectClinicPage(SelectClinicViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}