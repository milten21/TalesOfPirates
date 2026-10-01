using System.Linq;
using Godot;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Features.Lobby.Select.Widgets;

public partial class SecurityCodeEdit : LineEdit
{
    public override void _Ready()
    {
        TextChanged += KeepDigits;
    }

    private void KeepDigits(string text)
    {
        if (text.Length <= SecurityCode.LongestLength && text.All(SecurityCode.IsDigit))
        {
            return;
        }

        var caret = text.Take(CaretColumn).Count(SecurityCode.IsDigit);

        Text = new string(text.Where(SecurityCode.IsDigit).Take(SecurityCode.LongestLength).ToArray());
        CaretColumn = Mathf.Min(caret, SecurityCode.LongestLength);
    }
}
