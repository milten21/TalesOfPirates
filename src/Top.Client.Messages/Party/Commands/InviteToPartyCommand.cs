using System;

namespace Top.Client.Messages.Party.Commands
{
    public class InviteToPartyCommand : IServerCommand
    {
        public InviteToPartyCommand(string characterName)
        {
            CharacterName = characterName ?? throw new ArgumentNullException(nameof(characterName));
        }

        public string CharacterName { get; }
    }
}
