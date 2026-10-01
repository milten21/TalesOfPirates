using System;

namespace Top.Client.Messages.Account.Commands
{
    public class DeleteCharacterCommand : IServerCommand
    {
        public DeleteCharacterCommand(int characterIndex, string code)
        {
            if (characterIndex < 0 || characterIndex > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characterIndex), characterIndex, "A character index is a position in the reply's list.");
            }

            CharacterIndex = characterIndex;
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        public int CharacterIndex { get; }

        public string Code { get; }
    }
}
