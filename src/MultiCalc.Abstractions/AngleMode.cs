namespace MultiCalc.Abstractions;

/// <summary>What the trigonometric functions take their argument in.</summary>
/// <remarks>
/// This crosses the seam into the engine rather than being rewritten into the expression.
/// Converting degrees by textual substitution means inserting a multiply inside every
/// function call and then finding its closing bracket, which is a parser pretending not
/// to be one. The engine already knows where the argument is.
/// </remarks>
public enum AngleMode
{
    /// <summary>Degrees, which is what a pocket calculator does by default.</summary>
    Degrees,

    /// <summary>Radians.</summary>
    Radians,
}
