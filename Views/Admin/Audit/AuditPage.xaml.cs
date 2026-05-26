namespace UL_Optometry.Views.Admin.Audit;
using UL_Optometry.ViewModels.Admin;

public partial class AuditPage : ContentPage
{
	public AuditPage(AuditViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
		
}