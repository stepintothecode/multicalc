using MultiCalc.Domain.Sessions;

namespace MultiCalc.Ui.Services;

/// <summary>The one line of a calculator shown under its name in the instance list.</summary>
public static class SessionPreview
{
    /// <summary>What is typed, or the last result, or nothing at all.</summary>
    public static string For(CalculatorSession session)
    {
        if (!session.Draft.IsEmpty)
        {
            return session.Draft.ToDisplay();
        }

        return session.LastResult ?? string.Empty;
    }

    /// <summary>The line to show under a name in the instance list.</summary>
    public static string ForList(CalculatorSession session)
    {
        var text = For(session);

        return text.Length == 0 ? "Empty" : text;
    }
}
