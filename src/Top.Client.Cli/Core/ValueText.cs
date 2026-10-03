using System;
using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using Top.Client.Messages;

namespace Top.Client.Cli.Core;

public static class ValueText
{
    private const int MaximumDepth = 6;

    private const int MaximumItemCount = 8;

    private const string Omission = "...";

    public static string FromPoint(Vector2 point)
    {
        return string.Create(CultureInfo.InvariantCulture, $"({point.X:0.##}, {point.Y:0.##})");
    }

    public static string FromSeconds(TimeSpan time)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{time.TotalSeconds:0.##} s");
    }

    public static string FromEvent(IServerEvent serverEvent)
    {
        if (serverEvent == null)
        {
            throw new ArgumentNullException(nameof(serverEvent));
        }

        var text = new StringBuilder(serverEvent.GetType().Name).Append(' ');

        AppendProperties(text, serverEvent, 0);

        return text.ToString();
    }

    private static void AppendValue(StringBuilder text, object? value, int depth)
    {
        switch (value)
        {
            case null:
                text.Append("null");
                break;

            case string characters:
                text.Append('"').Append(characters).Append('"');
                break;

            case Vector2 point:
                text.Append(FromPoint(point));
                break;

            case float or double:
                text.Append(((IFormattable)value).ToString("0.##", CultureInfo.InvariantCulture));
                break;

            case Enum or bool:
                text.Append(value);
                break;

            case IFormattable formattable:
                text.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;

            case IEnumerable items:
                AppendItems(text, items, depth);
                break;

            default:
                AppendProperties(text, value, depth);
                break;
        }
    }

    private static void AppendItems(StringBuilder text, IEnumerable items, int depth)
    {
        if (depth >= MaximumDepth)
        {
            text.Append($"[{Omission}]");

            return;
        }

        var count = 0;

        text.Append('[');

        foreach (var item in items)
        {
            if (count < MaximumItemCount)
            {
                text.Append(count == 0 ? string.Empty : ", ");
                AppendValue(text, item, depth + 1);
            }

            count++;
        }

        if (count > MaximumItemCount)
        {
            text.Append($", {Omission} {count - MaximumItemCount} more");
        }

        text.Append(']');
    }

    private static void AppendProperties(StringBuilder text, object value, int depth)
    {
        if (depth >= MaximumDepth)
        {
            text.Append($"{{ {Omission} }}");

            return;
        }

        var separator = " ";

        text.Append('{');

        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            text.Append(separator).Append(property.Name).Append(" = ");
            AppendValue(text, property.GetValue(value), depth + 1);
            separator = ", ";
        }

        text.Append(" }");
    }
}
