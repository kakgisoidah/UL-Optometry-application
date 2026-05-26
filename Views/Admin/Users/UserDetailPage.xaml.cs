namespace UL_Optometry.Views.Admin.Users;
using UL_Optometry.ViewModels.Admin;
public partial class UserDetailPage : ContentPage
{
	public UserDetailPage(UserDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}