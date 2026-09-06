using System.Text;

namespace MultiCalc.Domain.Calculation;

/// <summary>
/// The expression as the display shows it, and the map between the two.
/// <para>
/// The display groups thousands and writes the operators with the glyphs people expect, so
/// the caret sitting between two characters on screen is rarely at the same offset in the
/// expression underneath. Anything that lets someone put a caret in the display has to be
/// able to translate both ways, which is what this is for.
/// </para>
/// </summary>
public sealed class ExpressionDisplay
{
    /// <summary>Characters the display writes differently from the expression underneath.</summary>
    private static readonly Dictionary<char, char> Glyphs = new()
    {
        ['*'] = '×',
        ['/'] = '÷',
        ['-'] = '−',

        // Euler's number is held as an uppercase E so nothing can confuse it with the "e"
        // inside a function name, but it is written lowercase.
        ['E'] = 'e',
    };

    /// <summary>The reverse, for text that comes back from the display.</summary>
    private static readonly Dictionary<char, char> RawGlyphs = new()
    {
        ['×'] = '*',
        ['÷'] = '/',
        ['−'] = '-',
        ['–'] = '-',
        ['—'] = '-',
    };

    /// <summary>Characters an expression may contain, beyond digits and letters.</summary>
    private const string Punctuation = ".()+-*/%^π";

    private static readonly ExpressionDisplay Blank = new(string.Empty, [0]);

    /// <summary>For each place a caret can sit on screen, the place it means in the expression.</summary>
    private readonly int[] rawIndexAt;

    private ExpressionDisplay(string text, int[] rawIndexAt)
    {
        Text = text;
        this.rawIndexAt = rawIndexAt;
    }

    /// <summary>What the display shows.</summary>
    public string Text { get; }

    /// <summary>Builds the display form of a raw expression.</summary>
    public static ExpressionDisplay For(string expression)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return Blank;
        }

        var text = new StringBuilder(expression.Length + 8);
        var origins = new List<int>(expression.Length + 8);
        var index = 0;

        while (index < expression.Length)
        {
            if (!char.IsAsciiDigit(expression[index]))
            {
                var c = expression[index];
                text.Append(Glyphs.TryGetValue(c, out var glyph) ? glyph : c);
                origins.Add(index);
                index++;

                continue;
            }

            var start = index;

            while (index < expression.Length && char.IsAsciiDigit(expression[index]))
            {
                index++;
            }

            var length = index - start;

            // Only the whole part of a number is grouped, never the digits after the point.
            var grouped = length > 3 && !(start > 0 && expression[start - 1] == '.');

            for (var i = 0; i < length; i++)
            {
                if (grouped && i > 0 && (length - i) % 3 == 0)
                {
                    // The separator belongs to the digit it precedes, so a caret either side
                    // of it lands somewhere sensible in the expression.
                    text.Append(',');
                    origins.Add(start + i);
                }

                text.Append(expression[start + i]);
                origins.Add(start + i);
            }
        }

        // One more than the characters: a caret can also sit after the last one.
        origins.Add(expression.Length);

        return new ExpressionDisplay(text.ToString(), [.. origins]);
    }

    /// <summary>Where in the expression a caret sitting at this place on screen belongs.</summary>
    public int RawIndex(int displayOffset) =>
        rawIndexAt[Math.Clamp(displayOffset, 0, Text.Length)];

    /// <summary>Where on screen a caret sitting at this place in the expression belongs.</summary>
    public int DisplayOffset(int rawIndex)
    {
        for (var offset = 0; offset < rawIndexAt.Length; offset++)
        {
            if (rawIndexAt[offset] >= rawIndex)
            {
                return offset;
            }
        }

        return Text.Length;
    }

    /// <summary>
    /// Turns text that came back from the display into an expression, and moves the caret
    /// with it. Anything an expression cannot contain is dropped rather than guessed at, so
    /// pasting a line from a document leaves the sum and not the prose around it.
    /// </summary>
    public static (string Expression, int Caret) FromDisplay(string text, int caret)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (string.Empty, 0);
        }

        var expression = new StringBuilder(text.Length);
        var moved = 0;
        var index = 0;

        while (index < text.Length)
        {
            // Letters are taken a word at a time: a word is a function name or it is prose,
            // and half of "please" is not something to leave in an expression.
            if (char.IsAsciiLetter(text[index]))
            {
                var start = index;

                while (index < text.Length && char.IsAsciiLetter(text[index]))
                {
                    index++;
                }

                var word = Word(text[start..index]);

                expression.Append(word);
                moved += Before(start, index, caret, word.Length);

                continue;
            }

            var kept = Keep(text[index]);

            if (kept is char c)
            {
                expression.Append(c);

                if (index < caret)
                {
                    moved++;
                }
            }

            index++;
        }

        return (expression.ToString(), moved);
    }

    /// <summary>What a run of letters in the display stands for, or nothing if it is prose.</summary>
    private static string Word(string letters)
    {
        // A lone "e" is Euler's number; letters that spell a function this calculator knows
        // are that function; anything else came in with a paste and is dropped.
        if (letters is "e" or "E")
        {
            return "E";
        }

        return ExpressionVocabulary.IsFunctionName(letters.ToLowerInvariant())
            ? letters.ToLowerInvariant()
            : string.Empty;
    }

    /// <summary>How much of a word sits before the caret, once the word is what it became.</summary>
    private static int Before(int start, int end, int caret, int length) =>
        caret >= end ? length : caret <= start ? 0 : Math.Min(caret - start, length);

    /// <summary>The expression character this display character stands for, or null to drop it.</summary>
    private static char? Keep(char c)
    {
        if (RawGlyphs.TryGetValue(c, out var raw))
        {
            return raw;
        }

        // Separators, spaces and anything else that wandered in with a paste are dropped.
        return char.IsAsciiDigit(c) || Punctuation.Contains(c) ? c : null;
    }
}
