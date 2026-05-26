namespace UL_Optometry.Views.Supervisor.Notification;

using UL_Optometry.ViewModels.Supervisor;

public partial class SupNotificationsPage : ContentPage
{
	public SupNotificationsPage(SupNotificationsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}