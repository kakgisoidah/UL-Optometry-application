namespace UL_Optometry.Views.Admin.Encounters;
using UL_Optometry.ViewModels.Admin;

public partial class AdminEncounterDetailPage : ContentPage
{
    public AdminEncounterDetailPage(AdminEncounterDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
