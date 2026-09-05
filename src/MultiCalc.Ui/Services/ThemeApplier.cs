using Microsoft.JSInterop;
using MultiCalc.Domain.Settings;

namespace MultiCalc.Ui.Services;

/// <summary>
/// Puts the chosen theme on the document root, so the stylesheet can paint the right palette.
/// "System" clears the attribute and lets the media query decide.
/// </summary>
public sealed class ThemeApplier
{
    private readonly IJSRuntime js;

    /// <summary>Creates the applier.</summary>
    public ThemeApplier(IJSRuntime js) => this.js = js;

    /// <summary>Applies a theme to the page.</summary>
    public async Task ApplyAsync(ThemePreference theme)
    {
        try
        {
            await js.InvokeVoidAsync("multicalc.setTheme", Name(theme)).ConfigureAwait(false);
        }
        catch (JSException)
        {
            // The web view can tear down mid navigation. A missed repaint is not worth a crash.
        }
        catch (TaskCanceledException)
        {
            // Same.
        }
    }

    private static string Name(ThemePreference theme) => theme switch
    {
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        _ => "system",
    };
}
