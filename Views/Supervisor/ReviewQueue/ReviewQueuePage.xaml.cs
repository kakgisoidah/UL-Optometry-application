namespace UL_Optometry.Views.Supervisor.ReviewQueue;

using UL_Optometry.ViewModels.Supervisor;

public partial class ReviewQueuePage : ContentPage
{
	private ReviewQueueViewModel? _vm;

	public ReviewQueuePage(ReviewQueueViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		_vm = vm;
		_vm.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(ReviewQueueViewModel.PendingCount))
				UpdateTabLabels();
		};
		UpdateTabLabels();
        SizeChanged += (s, e) => UpdateCellWidths();
	}

    private void UpdateCellWidths()
    {
        if (Width <= 0) return;
        double w = Width / 3;
        TabIndicator.WidthRequest = w;
    }

	private void UpdateTabLabels()
	{
		if (_vm is null) return;
		var count = _vm.PendingCount;
		PendingTab.Text  = count > 0 ? $"Pending Review ({count})" : "Pending Review";
		TodayTab.Text    = "Reviewed Today";
		AllTab.Text      = "All Cases";
	}

	private void OnPendingTab(object sender, EventArgs e) => SwitchTab(0);
	private void OnTodayTab(object sender, EventArgs e)   => SwitchTab(1);
	private void OnAllTab(object sender, EventArgs e)     => SwitchTab(2);

	private void SwitchTab(int i)
	{
		PendingPanel.IsVisible = i == 0;
		TodayPanel.IsVisible   = i == 1;
		AllPanel.IsVisible     = i == 2;
		SearchBar.IsVisible    = i == 0;

        double w = Width / 3;
		TabIndicator.TranslationX = i * w;

		void S(Button b, bool active)
		{
			b.TextColor  = active
				? (Color)Application.Current!.Resources["Primary"]
				: (Color)Application.Current!.Resources["Muted"];
			b.FontFamily = active ? "DMSansMedium" : "DMSans";
		}

		S(PendingTab, i == 0);
		S(TodayTab,   i == 1);
		S(AllTab,     i == 2);
	}
}
