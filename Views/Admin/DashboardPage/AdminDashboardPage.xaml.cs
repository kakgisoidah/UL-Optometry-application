namespace UL_Optometry.Views.Admin.DashboardPage;
using UL_Optometry.ViewModels.Admin;
public partial class AdminDashboardPage : ContentPage
{
	public AdminDashboardPage(AdminDashboardViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}