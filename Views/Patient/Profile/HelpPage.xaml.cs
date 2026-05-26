namespace UL_Optometry.Views.Patient.Profile;
using UL_Optometry.ViewModels.Patient;

public partial class HelpPage : ContentPage
{
	public HelpPage(HelpViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}