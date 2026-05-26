namespace UL_Optometry.Views.Admin.Scheduling;
using UL_Optometry.ViewModels.Admin;

public partial class SchedulingPage : ContentPage
{
	public SchedulingPage(SchedulingViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
	
}