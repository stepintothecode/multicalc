using Microsoft.Extensions.Logging;
using MultiCalc.Abstractions;

namespace MultiCalc.App.Services;

/// <summary>
/// Opens links in the device browser. <see cref="BrowserLaunchMode.External"/> is not a
/// preference: a payment page shown inside the app reads to both stores as an in-app
/// purchase that skipped their billing, and gets the app refused.
/// </summary>
public sealed class MauiExternalBrowser : IExternalBrowser
{
    private readonly ILogger<MauiExternalBrowser> logger;

    /// <summary>Creates the browser adapter.</summary>
    public MauiExternalBrowser(ILogger<MauiExternalBrowser> logger) => this.logger = logger;

    /// <inheritdoc />
    public async Task<bool> OpenAsync(string url)
    {
        try
        {
            return await Browser.Default.OpenAsync(url, BrowserLaunchMode.External).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not open {Url}", url);
            return false;
        }
    }
}
