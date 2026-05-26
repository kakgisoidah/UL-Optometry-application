namespace UL_Optometry.Views.Admin.Supervisors;
using UL_Optometry.ViewModels.Admin;

public partial class AdminSupervisorDetailPage : ContentPage
{
	public AdminSupervisorDetailPage(AdminSupervisorDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

	}
}