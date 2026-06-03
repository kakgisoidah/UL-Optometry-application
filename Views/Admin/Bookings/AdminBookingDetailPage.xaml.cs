using UL_Optometry.ViewModels.Admin;

namespace UL_Optometry.Views.Admin.Bookings;

public partial class AdminBookingDetailPage : ContentPage
{
    public AdminBookingDetailPage(AdminBookingDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is AdminBookingDetailViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}