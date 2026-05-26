namespace UL_Optometry.Views.Admin.Users;
using UL_Optometry.ViewModels.Admin;
public partial class UsersPage : ContentPage
{
	public UsersPage(UsersViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}