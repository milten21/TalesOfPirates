namespace Top.Client.Messages.Play.Events
{
    public class CharacterTeamLeaderChangedEvent : IServerEvent
    {
        public CharacterTeamLeaderChangedEvent(uint worldId, uint teamLeaderId)
        {
            WorldId = worldId;
            TeamLeaderId = teamLeaderId;
        }

        public uint WorldId { get; }

        public uint TeamLeaderId { get; }
    }
}
