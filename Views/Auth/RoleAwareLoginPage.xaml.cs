using UL_Optometry.ViewModels.Auth;

namespace UL_Optometry.Views.Auth;

public partial class RoleAwareLoginPage : ContentPage
{
	public RoleAwareLoginPage(LoginViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}