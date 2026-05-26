using UL_Optometry.ViewModels.Admin;

namespace UL_Optometry.Views.Admin.Scheduling;

public partial class BlockedDatesPage : ContentPage
{
    public BlockedDatesPage(BlockedDatesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as BlockedDatesViewModel)?.LoadCommand.Execute(null);
    }
}