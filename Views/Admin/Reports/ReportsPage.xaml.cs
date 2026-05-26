namespace UL_Optometry.Views.Admin.Reports;
using UL_Optometry.ViewModels.Admin;

public partial class ReportsPage : ContentPage
{
	public ReportsPage(ReportsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}