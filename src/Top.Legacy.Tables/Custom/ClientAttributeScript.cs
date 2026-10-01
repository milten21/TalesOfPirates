using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public struct ClientAttribute
    {
        public int PortraitAngle;
        public float PortraitDistance;
        public float PortraitHeight;
    }

    /// <summary>
    /// Reads CharacterConf.clu, the script that sets the client-only attributes
    /// of a character type with CHA_SetClientAttr: the angle, the distance and
    /// the height of its party portrait camera. It reads the statement shapes
    /// the shipped script uses: a direct call, a local list of numbers, and a
    /// call of the script's own SetClientAttr, which calls CHA_SetClientAttr
    /// for each type in a list. A later call for a type overwrites an earlier one.
    /// </summary>
    public class ClientAttributeScript
    {
        private const int ValueCount = 3;

        private static readonly Regex Statement = new Regex(
            @"--[^\n]*"
            + @"|local\s+(?<list>\w+)\s*=\s*\{(?<items>[^}]*)\}"
            + @"|CLU_Call\(\s*""CHA_SetClientAttr""\s*,(?<arguments>[^)]*)\)"
            + @"|(?<!function\s+)\bSetClientAttr\(\s*(?<types>\w+)\s*,\s*(?<values>\w+)\s*\)");

        private ClientAttributeScript(IReadOnlyDictionary<int, ClientAttribute> attributes)
        {
            Attributes = attributes;
        }

        public IReadOnlyDictionary<int, ClientAttribute> Attributes { get; }

        public static ClientAttributeScript Read(Stream stream)
        {
            var attributes = new Dictionary<int, ClientAttribute>();
            var lists = new Dictionary<string, string[]>();

            foreach (Match statement in Statement.Matches(string.Join("\n", Gbk.ReadLines(stream))))
            {
                if (statement.Groups["list"].Success)
                {
                    lists[statement.Groups["list"].Value] = SplitItems(statement.Groups["items"].Value);
                }
                else if (statement.Groups["arguments"].Success)
                {
                    var arguments = SplitItems(statement.Groups["arguments"].Value);

                    if (arguments.Length == ValueCount + 1 && int.TryParse(arguments[0], out var type)
                        && TryParseAttribute(arguments.Skip(1).ToArray(), out var attribute))
                    {
                        attributes[type] = attribute;
                    }
                }
                else if (lists.TryGetValue(statement.Groups["types"].Value, out var types)
                         && lists.TryGetValue(statement.Groups["values"].Value, out var values)
                         && TryParseAttribute(values, out var attribute))
                {
                    foreach (var typeText in types)
                    {
                        if (int.TryParse(typeText, out var type))
                        {
                            attributes[type] = attribute;
                        }
                    }
                }
            }

            return new ClientAttributeScript(attributes);
        }

        private static string[] SplitItems(string items)
        {
            return items.Split(',').Select(item => item.Trim()).Where(item => item.Length > 0).ToArray();
        }

        private static bool TryParseAttribute(string[] values, out ClientAttribute attribute)
        {
            attribute = default;

            if (values.Length != ValueCount
                || !int.TryParse(values[0], out var angle)
                || !float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var distance)
                || !float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                return false;
            }

            attribute = new ClientAttribute
            {
                PortraitAngle = angle, PortraitDistance = distance, PortraitHeight = height,
            };

            return true;
        }
    }
}
