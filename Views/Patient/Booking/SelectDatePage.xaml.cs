namespace UL_Optometry.Views.Patient.Booking;
using
	 UL_Optometry.ViewModels.Patient;
public partial class SelectDatePage : ContentPage
{
	public SelectDatePage(SelectDateViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}