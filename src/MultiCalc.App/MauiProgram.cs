using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;
using MultiCalc.App.Platforms.Android;
using MultiCalc.App.Services;
using MultiCalc.Domain.Calculation;
using MultiCalc.Domain.Sessions;
using MultiCalc.Domain.Settings;
using MultiCalc.Evaluation;
using MultiCalc.Storage;
using MultiCalc.Ui.Services;

namespace MultiCalc.App;

/// <summary>Composition root. Every seam is bound to a real implementation here and nowhere else.</summary>
public static class MauiProgram
{
    /// <summary>Builds and configures the application.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        builder.Services.AddMauiBlazorWebView();
        TrimWebViewChrome();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Logging.SetMinimumLevel(LogLevel.Information);

        AddPlatform(builder.Services);
        AddCore(builder.Services);
        AddUi(builder.Services);

        var app = builder.Build();

        Start(app.Services);

        return app;
    }

    /// <summary>The seams that only exist on a device.</summary>
    private static void AddPlatform(IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IHaptics, AndroidHaptics>();
        services.AddSingleton<IAppBuildInfo, MauiAppBuildInfo>();
        services.AddSingleton<IExternalBrowser, MauiExternalBrowser>();
        services.AddSingleton(FileSaver.Default);
        services.AddSingleton<IFileExporter, MauiFileExporter>();
        services.AddSingleton<IFileReader, MauiFileReader>();
        services.AddSingleton<INativeCalculatorLauncher, AndroidNativeCalculatorLauncher>();
    }

    /// <summary>Arithmetic and storage. Both are plain .NET and are covered by the unit tests.</summary>
    private static void AddCore(IServiceCollection services)
    {
        var dataDirectory = FileSystem.AppDataDirectory;

        services.AddSingleton<ICalculatorEngine, NCalcCalculatorEngine>();
        services.AddSingleton<CalculationService>();
        services.AddSingleton<ISessionStore>(_ => new JsonSessionStore(dataDirectory));
        services.AddSingleton<ISettingsStore>(_ => new JsonSettingsStore(dataDirectory));
    }

    /// <summary>
    /// UI state. Singletons on purpose: the open calculators have to survive navigating to
    /// Settings and back.
    /// </summary>
    private static void AddUi(IServiceCollection services)
    {
        services.AddSingleton<SettingsState>();
        services.AddSingleton<CalculatorState>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<BackNavigation>();
        services.AddScoped<ThemeApplier>();
    }

    /// <summary>
    /// Turns off the web view's own scrollbars and its overscroll glow. CSS cannot reach
    /// those, because the host view draws them rather than the page.
    /// </summary>
    private static void TrimWebViewChrome() =>
        Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebViewHandler.BlazorWebViewMapper
            .AppendToMapping("TrimChrome", (handler, _) =>
            {
                var view = handler.PlatformView;
                view.OverScrollMode = global::Android.Views.OverScrollMode.Never;
                view.VerticalScrollBarEnabled = false;
                view.HorizontalScrollBarEnabled = false;
                view.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            });

    /// <summary>
    /// Reads the settings and the saved calculators before the first frame, so the app never
    /// paints the wrong theme and then corrects itself. A failure here is logged rather than
    /// thrown: bad stored data must not stop the app opening.
    /// </summary>
    private static void Start(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        try
        {
            services.GetRequiredService<SettingsState>().InitialiseAsync().GetAwaiter().GetResult();
            services.GetRequiredService<CalculatorState>().InitialiseAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not restore the saved state; starting fresh");
        }
    }
}
