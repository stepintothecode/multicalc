using MultiCalc.Ui.Services;

namespace MultiCalc.App;

/// <summary>Hosts the Blazor UI and hands the phone's back press to it.</summary>
public partial class MainPage : ContentPage
{
    private readonly BackNavigation back;

    /// <summary>Creates the page.</summary>
    public MainPage(BackNavigation back)
    {
        InitializeComponent();
        this.back = back;
    }

    /// <summary>
    /// Gives the UI first refusal on back: a sheet closes, a page returns to the calculator,
    /// and only a press with nothing left to dismiss closes the app.
    /// </summary>
    protected override bool OnBackButtonPressed() => back.Handle() || base.OnBackButtonPressed();
}
