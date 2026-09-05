using MultiCalc.Domain.Settings;
using MultiCalc.Ui.Services;

namespace MultiCalc.App;

/// <summary>The MAUI application. Owns the window and keeps the native shell in step with the settings.</summary>
public partial class App : Application
{
    private readonly CalculatorState calculator;
    private readonly SettingsState settings;
    private readonly BackNavigation back;

    /// <summary>Creates the application.</summary>
    public App(CalculatorState calculator, SettingsState settings, BackNavigation back)
    {
        InitializeComponent();

        this.calculator = calculator;
        this.settings = settings;
        this.back = back;

        settings.Changed += ApplyNativeTheme;
        ApplyNativeTheme();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage(back)) { Title = "MultiCalc" };

        // Android kills processes without warning, so the last state is written when the
        // window stops rather than relying on the debounced save having fired.
        window.Stopped += (_, _) => _ = calculator.FlushAsync();

        return window;
    }

    /// <summary>
    /// The web view paints itself from CSS, but the status bar and the window background are
    /// native and follow this.
    /// </summary>
    private void ApplyNativeTheme() => UserAppTheme = settings.Current.Theme switch
    {
        ThemePreference.Light => AppTheme.Light,
        ThemePreference.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified,
    };
}
