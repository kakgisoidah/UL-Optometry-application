namespace UL_Optometry.Views.Admin.Students;
using UL_Optometry.ViewModels.Admin;

public partial class AdminStudentDetailPage : ContentPage
{
	public AdminStudentDetailPage(AdminStudentDetailViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as AdminStudentDetailViewModel)?.LoadCommand.Execute(null);
    }
}