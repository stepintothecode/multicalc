namespace MultiCalc.Abstractions;

/// <summary>The short buzz a key press gives. Silent where the device has no vibrator.</summary>
public interface IHaptics
{
    /// <summary>A light tap, for a key press.</summary>
    void Tap();
}
