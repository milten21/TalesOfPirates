using System;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class CharacterAppearedEvent : IServerEvent
    {
        public CharacterAppearedEvent(CharacterState character, SeeType seeType)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            SeeType = seeType;
        }

        public CharacterState Character { get; }

        public SeeType SeeType { get; }
    }
}
