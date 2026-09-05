namespace MultiCalc.Abstractions;

/// <summary>Opens a link in the device's own browser.</summary>
/// <remarks>
/// It has to be the real browser, never an in-app web view. Both stores read a payment page
/// inside a web view as an in-app purchase that skipped their billing, and refuse the app.
/// </remarks>
public interface IExternalBrowser
{
    /// <summary>Opens <paramref name="url"/> outside the app. Returns false if nothing could open it.</summary>
    Task<bool> OpenAsync(string url);
}
