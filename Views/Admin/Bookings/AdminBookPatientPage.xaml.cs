using UL_Optometry.ViewModels.Admin;

namespace UL_Optometry.Views.Admin.Bookings;

public partial class AdminBookPatientPage : ContentPage
{
    private AdminBookPatientViewModel? _vm;

    public AdminBookPatientPage(AdminBookPatientViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AdminBookPatientViewModel.CurrentStep))
                UpdatePanels(vm.CurrentStep);
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm?.LoadCommand.Execute(null);
        UpdatePanels(_vm?.CurrentStep ?? 1);
    }

    private void UpdatePanels(int step)
    {
        Step1Panel.IsVisible = step == 1;
        Step2Panel.IsVisible = step == 2;
        Step3Panel.IsVisible = step == 3;
        Step4Panel.IsVisible = step == 4;
        Step5Panel.IsVisible = step == 5;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_vm is null) return base.OnBackButtonPressed();
        if (_vm.CurrentStep > 1)
        {
            _vm.PreviousStepCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}