namespace Top.Client.Messages.Party.Commands
{
    public class AcceptInviteCommand : IServerCommand
    {
        public AcceptInviteCommand(uint inviterId)
        {
            InviterId = inviterId;
        }

        public uint InviterId { get; }
    }
}
