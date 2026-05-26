namespace UL_Optometry.Views.Supervisor.ReviewQueue;
using UL_Optometry.ViewModels.Supervisor;
public partial class ApprovalSuccessPage : ContentPage
{
	public ApprovalSuccessPage(ApprovalSuccessViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
	
}