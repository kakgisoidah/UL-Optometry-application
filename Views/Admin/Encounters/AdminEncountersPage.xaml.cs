namespace UL_Optometry.Views.Admin.Encounters;
using UL_Optometry.ViewModels.Admin;

public partial class AdminEncountersPage : ContentPage
{
    const double W = 72.0;  // tab width for indicator translation

    public AdminEncountersPage(AdminEncountersViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as AdminEncountersViewModel)?.LoadCommand.Execute(null);
    }

    // ── Tab button click handlers ─────────────────────────────────────
  //  private void OnAllTab(object s, EventArgs e) => Switch(0);
 //   private void OnRevTab(object s, EventArgs e) => Switch(1);
    //private void OnAppTab(object s, EventArgs e) => Switch(2);
    //private void OnRevReqTab(object s, EventArgs e) => Switch(3);
  //  private void OnDraftTab(object s, EventArgs e) => Switch(4);

    // ── Visual tab switching (no business logic — stays in code-behind) ─
   

    private void OnRevTab(object sender, EventArgs e)
    {
        Switch(1);
    }

    private void OnAllTab(object sender, EventArgs e)
    {
        Switch(0);

    }

    private void OnAppTab(object sender, EventArgs e)
    {
        Switch(2);
    }

    private void OnRevReqTab(object sender, EventArgs e)
    {
        Switch(3);
    }

    private void OnDraftTab(object sender, EventArgs e)
    {
        Switch(4);
    } 
    
    
    private void Switch(int i)
    {
        // Show only the active panel
        AllEPanel.IsVisible = i == 0;
        RevEPanel.IsVisible = i == 1;
        AppEPanel.IsVisible = i == 2;
        RevReqPanel.IsVisible = i == 3;
        DraftEPanel.IsVisible = i == 4;

        // Slide the underline indicator
        ETabBar.TranslationX = i * W;

        // Style active/inactive tab buttons
        void S(Button b, bool active)
        {
            var key = active ? "Primary" : "Muted";
            if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
                b.TextColor = (Color)color;
            b.FontFamily = active ? "DMSansMedium" : "DMSans";
        }

        S(AllETab, i == 0);
        S(RevETab, i == 1);
        S(AppETab, i == 2);
        S(RevReqTab, i == 3);
        S(DraftETab, i == 4);
    }
}