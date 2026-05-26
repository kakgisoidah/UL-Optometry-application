namespace UL_Optometry.Views.Admin.PoE;
using UL_Optometry.ViewModels.Admin;

public partial class AdminPoePage : ContentPage
{
	public AdminPoePage(AdminPoeViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
    }
}