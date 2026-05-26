namespace UL_Optometry.Views.Admin.Supervisors;
using UL_Optometry.ViewModels.Admin;

public partial class AdminSupervisorsPage : ContentPage
{
	public AdminSupervisorsPage(AdminSupervisorsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}