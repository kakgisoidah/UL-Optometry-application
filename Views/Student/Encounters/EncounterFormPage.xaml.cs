namespace UL_Optometry.Views.Student.Encounters;
using UL_Optometry.ViewModels.Student;
public partial class EncounterFormPage : ContentPage
{
    private EncounterFormViewModel? _vm;
    public EncounterFormPage(EncounterFormViewModel vm)
	{
		InitializeComponent();
        BindingContext = _vm = vm;

        // Subscribe to step changes so panels update
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EncounterFormViewModel.CurrentStep))
                UpdatePanelVisibility(vm.CurrentStep);
        };

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm?.LoadCommand.Execute(null);
        UpdatePanelVisibility(_vm?.CurrentStep ?? 1);
    }

    private void UpdatePanelVisibility(int step)
    {
        // Only the active step panel is visible
        Step1Panel.IsVisible = step == 1;
        Step2Panel.IsVisible = step == 2;
        Step3Panel.IsVisible = step == 3;
        Step4Panel.IsVisible = step == 4;
        Step5Panel.IsVisible = step == 5;
        Step6Panel.IsVisible = step == 6;
    }

    // Prevent accidental back-navigation mid-form
    protected override bool OnBackButtonPressed()
    {
        if (_vm is null) return base.OnBackButtonPressed();

        // If not on step 1 — go back one step instead of leaving the form
        if (_vm.CurrentStep > 1)
        {
            _vm.PreviousStepCommand.Execute(null);
            return true; // consumed
        }

        // On step 1 — prompt the user
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool leave = await DisplayAlert(
                "Leave Encounter?",
                "Your progress will be lost unless you save a draft first.",
                "Leave", "Stay");
            if (leave) await Shell.Current.GoToAsync("..");
        });
        return true; // always consumed
    }
}