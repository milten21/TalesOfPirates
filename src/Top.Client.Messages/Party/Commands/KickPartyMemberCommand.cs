namespace Top.Client.Messages.Party.Commands
{
    public class KickPartyMemberCommand : IServerCommand
    {
        public KickPartyMemberCommand(uint memberId)
        {
            MemberId = memberId;
        }

        public uint MemberId { get; }
    }
}
