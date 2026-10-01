using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class CharacterDisappearedEvent : IServerEvent
    {
        public CharacterDisappearedEvent(uint worldId, SeeType seeType)
        {
            WorldId = worldId;
            SeeType = seeType;
        }

        public uint WorldId { get; }

        public SeeType SeeType { get; }
    }
}
