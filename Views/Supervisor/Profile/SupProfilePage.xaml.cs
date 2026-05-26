namespace UL_Optometry.Views.Supervisor.Profile;
using Microsoft.Maui.Controls;
using UL_Optometry.ViewModels.Supervisor;
public partial class SupProfilePage : ContentPage
{
	public SupProfilePage(SupProfileViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}