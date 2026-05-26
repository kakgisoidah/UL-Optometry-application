namespace UL_Optometry.Views.Patient.Booking;

using UL_Optometry.ViewModels.Patient;

public partial class SelectSlotPage : ContentPage
{
	public SelectSlotPage(SelectSlotViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}