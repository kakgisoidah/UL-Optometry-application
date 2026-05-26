namespace UL_Optometry.Views.Admin.Users;
using UL_Optometry.ViewModels.Admin;
public partial class AddUserPage : ContentPage
{
	public AddUserPage(AddUserViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}