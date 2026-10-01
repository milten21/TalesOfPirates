using System;

namespace Top.Client.Messages.Play.Commands
{
    public class BeginPlayCommand : IServerCommand
    {
        public BeginPlayCommand(int characterIndex)
        {
            if (characterIndex < 0 || characterIndex > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characterIndex), characterIndex, "A character index is a position in the reply's list.");
            }

            CharacterIndex = characterIndex;
        }

        public int CharacterIndex { get; }
    }
}
