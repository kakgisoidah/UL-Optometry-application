using UL_Optometry.ViewModels.Auth;

namespace UL_Optometry.Views.Auth;

public partial class CreateAccountPage : ContentPage
{
	public CreateAccountPage(CreateAccountViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}