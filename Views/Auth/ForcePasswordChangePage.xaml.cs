namespace UL_Optometry.Views.Auth;
using UL_Optometry.ViewModels.Auth;
public partial class ForcePasswordChangePage : ContentPage
{
	public ForcePasswordChangePage(ForcePasswordChangeViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
	}

    protected override bool OnBackButtonPressed() => true;
}