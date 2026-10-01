using Top.Client.Messages.Events;
using Top.Logging;

namespace Top.Client.Core;

public class ConnectionWatch(Session session, SceneRouter router)
{
    public void ReportLostConnection(DisconnectedEvent disconnected)
    {
        if (disconnected.Reason == DisconnectReason.ClientClosed || session.CurrentLogin == null)
        {
            return;
        }

        Log.Warning($"the connection to {session.CurrentServer} was lost ({disconnected.Reason})");

        router.LeaveServer(Disconnection.ToText(disconnected.Reason));
    }
}
