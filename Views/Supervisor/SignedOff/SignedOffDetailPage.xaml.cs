namespace UL_Optometry.Views.Supervisor.SignedOff;
using Microsoft.Maui.Controls;
using UL_Optometry.ViewModels.Supervisor;

public partial class SignedOffDetailPage : ContentPage
{
	public SignedOffDetailPage(SignedOffDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}