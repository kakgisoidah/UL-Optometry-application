namespace UL_Optometry.Views.Supervisor.ReviewQueue;
using UL_Optometry.ViewModels.Supervisor;

public partial class EncounterReviewPage : ContentPage
{
	public EncounterReviewPage(EncounterReviewViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}