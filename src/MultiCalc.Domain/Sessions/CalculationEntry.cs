namespace MultiCalc.Domain.Sessions;

/// <summary>One finished calculation, kept for the history tape.</summary>
/// <param name="Expression">What was typed, in display form.</param>
/// <param name="Result">What it came to, in display form.</param>
/// <param name="At">When equals was pressed.</param>
public sealed record CalculationEntry(string Expression, string Result, DateTimeOffset At);
