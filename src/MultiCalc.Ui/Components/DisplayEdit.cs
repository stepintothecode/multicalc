namespace MultiCalc.Ui.Components;

/// <summary>
/// An edit made in the display itself rather than on the keypad: what the display now
/// reads, written the way the display writes it, and where the caret was left.
/// </summary>
public sealed record DisplayEdit(string Text, int Caret);
