using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public record GemHint(string Text, int Factor);

    /// <summary>
    /// Reads the gem hint functions of scripts/lua/table/scripts.lua, which
    /// stoneinfo names per gem. A hint function multiplies the gem level by a
    /// factor and appends the product to a text:
    /// <c>local eff = Lv * 4; local Hint = "Gem Bonus Attack +" .. eff</c>.
    /// A function of any other shape is not a hint.
    /// </summary>
    public class GemHintScript
    {
        private static readonly Regex HintFunction = new Regex(
            @"function\s+(?<name>\w+)\s*\(\s*(?<level>\w+)\s*\)\s*"
            + @"local\s+(?<effect>\w+)\s*=\s*\k<level>\s*\*\s*(?<factor>-?\d+)\s*"
            + @"local\s+(?<hint>\w+)\s*=\s*""(?<text>[^""]*)""\s*\.\.\s*\k<effect>\s*"
            + @"return\s+\k<hint>\s+end");

        private GemHintScript(IReadOnlyDictionary<string, GemHint> hints)
        {
            Hints = hints;
        }

        public IReadOnlyDictionary<string, GemHint> Hints { get; }

        public static GemHintScript Read(Stream stream)
        {
            var hints = new Dictionary<string, GemHint>();

            foreach (Match function in HintFunction.Matches(string.Join("\n", Gbk.ReadLines(stream))))
            {
                hints[function.Groups["name"].Value] = new GemHint(function.Groups["text"].Value,
                    int.Parse(function.Groups["factor"].Value, CultureInfo.InvariantCulture));
            }

            return new GemHintScript(hints);
        }
    }
}
