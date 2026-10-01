using Top.Client.Messages.Events;

namespace Top.Client.Core;

public static class Disconnection
{
    public const string Title = "Connection lost";

    public static string ToText(DisconnectReason reason)
    {
        return reason switch
        {
            DisconnectReason.ClientClosed => "The client closed the connection.",
            DisconnectReason.Unreachable => "The client could not reach the server.",
            DisconnectReason.HandshakeFailed => "The server did not accept an encrypted connection.",
            DisconnectReason.ServerClosed => "The server closed the connection.",
            DisconnectReason.TimedOut => "The server did not answer in time.",
            DisconnectReason.Broken => "The connection to the server was lost.",
            _ => $"The connection closed ({reason})."
        };
    }
}
