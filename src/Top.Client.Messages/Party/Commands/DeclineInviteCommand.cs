namespace Top.Client.Messages.Party.Commands
{
    public class DeclineInviteCommand : IServerCommand
    {
        public DeclineInviteCommand(uint inviterId)
        {
            InviterId = inviterId;
        }

        public uint InviterId { get; }
    }
}
