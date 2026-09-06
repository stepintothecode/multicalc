using MultiCalc.Abstractions;

namespace MultiCalc.Domain.Settings;

/// <summary>
/// Everything the settings page can change, which is deliberately not much. Anything the
/// main screen can offer as a button does not need a setting as well.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Which palette to paint in.</summary>
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    /// <summary>Whether keys give a short vibration.</summary>
    public bool HapticFeedback { get; init; } = true;

    /// <summary>
    /// What the trigonometric keys measure in. App wide rather than per calculator,
    /// because it is a unit you work in, not a layout you choose.
    /// </summary>
    public AngleMode Angles { get; init; } = AngleMode.Degrees;

    /// <summary>The settings a fresh install starts with.</summary>
    public static AppSettings Default { get; } = new();
}
