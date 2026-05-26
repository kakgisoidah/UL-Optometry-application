namespace UL_Optometry.Views.Auth;
using UL_Optometry.ViewModels.Auth;
public partial class ForgotPasswordPage : ContentPage
{
	public ForgotPasswordPage(ForgotPasswordViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}