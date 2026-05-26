namespace UL_Optometry.Views.Admin.Settings;
using UL_Optometry.ViewModels.Admin;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
		
}