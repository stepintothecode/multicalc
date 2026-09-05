namespace MultiCalc.Domain.Sessions;

/// <summary>
/// The colour a calculator is tagged with, so the one you are on is recognisable at a
/// glance rather than by reading its name.
/// <para>
/// A hue, not a colour. Saturation and lightness are decided by the stylesheet, separately
/// for the light and the dark theme, which is what keeps every choice readable on both
/// backgrounds. A picker that handed back a free hex could produce pale yellow on white or
/// navy on black, and there would be nothing the app could do about it.
/// </para>
/// </summary>
public readonly record struct CalculatorTint
{
    /// <summary>Degrees around the colour wheel.</summary>
    public const int Wheel = 360;

    private CalculatorTint(int hue) => Hue = hue;

    /// <summary>The hue, from 0 to 359.</summary>
    public int Hue { get; }

    /// <summary>
    /// The quick choices in the picker, one every fifteen degrees. Twenty four of them, so
    /// every calculator the app allows can start out a different colour.
    /// </summary>
    public static IReadOnlyList<CalculatorTint> Palette { get; } =
        [.. Enumerable.Range(0, 24).Select(step => new CalculatorTint(step * 15))];

    /// <summary>The tint a calculator gets when nothing else says otherwise.</summary>
    public static CalculatorTint Default { get; } = new(170);

    /// <summary>Wraps any number onto the wheel, so no caller has to range check first.</summary>
    public static CalculatorTint FromHue(int hue) => new(((hue % Wheel) + Wheel) % Wheel);

    /// <summary>The custom property the stylesheet reads to build the tint.</summary>
    public string Style => $"--hue:{Hue}";

    /// <inheritdoc />
    public override string ToString() => Hue.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
