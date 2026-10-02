using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Top.Client.LegacyBridge;

namespace Top.Client.Tools.Debug.Packets;

public static class PacketText
{
    private const int IndentWidth = 2;

    private const int MaxDepth = 6;

    private const int MaxItems = 64;

    public static string Time(ObservedPacket packet)
    {
        return packet.Time.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    public static string Direction(ObservedPacket packet)
    {
        return packet.Direction == PacketDirection.Incoming ? "In" : "Out";
    }

    public static string Opcode(ObservedPacket packet)
    {
        return OpcodeNameLookup.Find(packet.Opcode) ?? packet.Opcode.ToString(CultureInfo.InvariantCulture);
    }

    public static string Size(ObservedPacket packet)
    {
        return packet.Bytes.Length.ToString(CultureInfo.InvariantCulture);
    }

    public static string Messages(ObservedPacket packet)
    {
        return packet.Messages.Count switch
        {
            0 => Reason(packet.EmptyReason),
            1 => packet.Messages[0].GetType().Name,
            _ => $"{packet.Messages[0].GetType().Name} +{packet.Messages.Count - 1}"
        };
    }

    public static bool MatchesOpcode(ObservedPacket packet, string filter)
    {
        if (ushort.TryParse(filter, NumberStyles.None, CultureInfo.InvariantCulture, out var opcode))
        {
            return packet.Opcode == opcode;
        }

        return OpcodeNameLookup.Find(packet.Opcode)?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static string Details(ObservedPacket packet)
    {
        var text = new StringBuilder();

        text.AppendLine($"{Direction(packet)}  {Opcode(packet)} ({packet.Opcode})  {Time(packet)}  {Size(packet)} bytes");
        text.AppendLine();

        if (packet.Messages.Count == 0)
        {
            text.AppendLine(Reason(packet.EmptyReason));
        }

        foreach (var message in packet.Messages)
        {
            text.AppendLine(message.GetType().Name);
            AppendProperties(text, message, 1);
        }

        return text.ToString();
    }

    private static string Reason(EmptyPacketReason? reason)
    {
        return reason switch
        {
            EmptyPacketReason.NoDecoder => "no decoder",
            EmptyPacketReason.DecodedToNothing => "decoded to nothing",
            _ => string.Empty
        };
    }

    private static void AppendProperties(StringBuilder text, object value, int depth)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            AppendValue(text, property.Name, ReadProperty(property, value), depth);
        }
    }

    private static object? ReadProperty(PropertyInfo property, object owner)
    {
        try
        {
            return property.GetValue(owner);
        }
        catch (TargetInvocationException failure)
        {
            return $"<{failure.InnerException?.GetType().Name}>";
        }
    }

    private static void AppendValue(StringBuilder text, string name, object? value, int depth)
    {
        text.Append(' ', depth * IndentWidth).Append(name).Append(": ");

        if (value == null || IsScalar(value))
        {
            text.AppendLine(FormatScalar(value));

            return;
        }

        if (depth >= MaxDepth)
        {
            text.AppendLine("...");

            return;
        }

        if (value is IEnumerable items)
        {
            AppendItems(text, items.Cast<object?>().ToList(), depth);

            return;
        }

        text.AppendLine(value.GetType().Name);
        AppendProperties(text, value, depth + 1);
    }

    private static void AppendItems(StringBuilder text, IList items, int depth)
    {
        text.AppendLine($"[{items.Count}]");

        for (var index = 0; index < Math.Min(items.Count, MaxItems); index++)
        {
            AppendValue(text, $"[{index}]", items[index], depth + 1);
        }

        if (items.Count > MaxItems)
        {
            text.Append(' ', (depth + 1) * IndentWidth).AppendLine($"... {items.Count - MaxItems} more");
        }
    }

    private static bool IsScalar(object value)
    {
        return value is string or Enum or IFormattable || value.GetType().IsPrimitive;
    }

    private static string FormatScalar(object? value)
    {
        return value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
