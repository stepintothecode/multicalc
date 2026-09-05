using MultiCalc.Domain.Sessions;

namespace MultiCalc.Domain.History;

/// <summary>Folds imported entries into the ones already there.</summary>
public static class HistoryMerge
{
    /// <summary>
    /// Combines two histories, newest first. Importing the same file twice is a no-op:
    /// entries matching on expression, result and timestamp are treated as the same
    /// calculation and kept once.
    /// </summary>
    public static IReadOnlyList<CalculationEntry> Combine(
        IReadOnlyList<CalculationEntry> existing,
        IReadOnlyList<CalculationEntry> incoming)
    {
        var seen = new HashSet<(string, string, DateTimeOffset)>();
        var combined = new List<CalculationEntry>(existing.Count + incoming.Count);

        foreach (var entry in existing.Concat(incoming))
        {
            if (seen.Add((entry.Expression, entry.Result, entry.At)))
            {
                combined.Add(entry);
            }
        }

        return combined
            .OrderByDescending(entry => entry.At)
            .Take(CalculatorSession.HistoryLimit)
            .ToList();
    }
}
