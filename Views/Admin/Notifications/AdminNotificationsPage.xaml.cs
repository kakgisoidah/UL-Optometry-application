namespace UL_Optometry.Views.Admin.Notifications;
using UL_Optometry.ViewModels.Admin;

public partial class AdminNotificationsPage : ContentPage
{
	public AdminNotificationsPage(AdminNotificationsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}