namespace UL_Optometry.Views.Patient.Booking;
using UL_Optometry.ViewModels.Patient;

public partial class SelectDatePage : ContentPage
{
	public SelectDatePage(SelectDateViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
        SizeChanged += OnSizeChanged;
        CalendarFlex.ChildAdded += (s, e) => UpdateCellWidths();
	}

    private void OnSizeChanged(object? sender, EventArgs e) => UpdateCellWidths();

    private void UpdateCellWidths()
    {
        if (Width <= 0) return;
        double cellWidth = Math.Floor((Width - 16) / 7); // 16 = ScrollView padding (8+8)
        foreach (var child in CalendarFlex.Children)
        {
            if (child is Grid g)
                g.WidthRequest = cellWidth;
        }
    }
}