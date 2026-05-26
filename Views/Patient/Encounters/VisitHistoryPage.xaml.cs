namespace UL_Optometry.Views.Patient.Encounters;

using UL_Optometry.ViewModels.Patient;

public partial class VisitHistoryPage : ContentPage
{
	public VisitHistoryPage(VisitHistoryViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}