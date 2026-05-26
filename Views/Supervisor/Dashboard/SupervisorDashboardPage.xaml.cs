namespace UL_Optometry.Views.Supervisor.Dashboard;
using UL_Optometry.ViewModels.Supervisor;

public partial class SupervisorDashboardPage : ContentPage
{
	public SupervisorDashboardPage(SupervisorDashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}